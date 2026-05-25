using Amazon.S3;
using Amazon.S3.Model;
using Elastic.Clients.Elasticsearch;
using ISDOX.DMS.Application.Interfaces;
using ISDOX.DMS.Domain.Entities;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using UglyToad.PdfPig;

namespace ISDOX.DMS.Worker
{
    public class BulkImportBackgroundService : BackgroundService
    {
        private readonly IConnection _rabbitMqConnection;
        private readonly IAmazonS3 _s3Client;
        private readonly ElasticsearchClient _elasticClient;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BulkImportBackgroundService> _logger;

        public BulkImportBackgroundService(
            IConnection rabbitMqConnection,
            IAmazonS3 s3Client,
            ElasticsearchClient elasticClient,
            IServiceProvider serviceProvider,
            ILogger<BulkImportBackgroundService> logger)
        {
            _rabbitMqConnection = rabbitMqConnection;
            _s3Client = s3Client;
            _elasticClient = elasticClient;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var channel = await _rabbitMqConnection.CreateChannelAsync(cancellationToken: stoppingToken);

            await channel.QueueDeclareAsync(
                queue: "bulk-import-queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: stoppingToken);

            await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var jobData = JsonSerializer.Deserialize<BulkImportMessage>(message);

                if (jobData == null)
                {
                    await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                    return;
                }

                try
                {
                    _logger.LogInformation($"Starting extraction for Job {jobData.JobId}");
                    await ProcessZipAsync(jobData, stoppingToken);

                    await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Critical failure processing Job {jobData.JobId}");
                    await MarkJobFailedAsync(jobData.JobId, ex.Message, stoppingToken);

                    await channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                }
            };

            await channel.BasicConsumeAsync(
                queue: "bulk-import-queue",
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async Task ProcessZipAsync(BulkImportMessage jobData, CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IDmsDbContext>();

            var job = await dbContext.BulkImportJobs.FindAsync(new object[] { jobData.JobId }, ct);
            if (job == null) return;

            job.Status = "Processing";
            await dbContext.SaveChangesAsync(ct);

            var tempDir = Path.Combine(Path.GetTempPath(), "ISDOX_Worker");
            Directory.CreateDirectory(tempDir);

            var localZipPath = Path.Combine(tempDir, $"{jobData.JobId}.zip");
            var extractPath = Path.Combine(tempDir, jobData.JobId.ToString());

            try
            {
                using (var response = await _s3Client.GetObjectAsync("documents", jobData.TempS3Key, ct))
                {
                    await response.WriteResponseStreamToFileAsync(localZipPath, false, ct);
                }

                ZipFile.ExtractToDirectory(localZipPath, extractPath);

                var files = Directory.GetFiles(extractPath, "*.*", SearchOption.AllDirectories)
                                     .Where(f => !new FileInfo(f).Attributes.HasFlag(FileAttributes.Hidden))
                                     .ToArray();

                job.TotalFiles = files.Length;
                await dbContext.SaveChangesAsync(ct);

                foreach (var filePath in files)
                {
                    var fileInfo = new FileInfo(filePath);
                    var documentId = Guid.NewGuid();
                    var s3DestKey = $"documents/{jobData.User}/{documentId}{fileInfo.Extension}";

                    using (var fs = fileInfo.OpenRead())
                    {
                        var putRequest = new PutObjectRequest
                        {
                            BucketName = "documents",
                            Key = s3DestKey,
                            InputStream = fs
                        };
                        await _s3Client.PutObjectAsync(putRequest, ct);
                    }

                    var newDocument = new Document
                    {
                        Id = documentId,
                        Name = fileInfo.Name,
                        Owner = jobData.User,
                        CreatedAt = DateTime.UtcNow
                    };

                    var newVersion = new DocumentVersion
                    {
                        Id = Guid.NewGuid(),
                        DocumentId = documentId,
                        VersionNumber = 1,
                        StoragePath = s3DestKey,
                        FileExtension = fileInfo.Extension,
                        FileSize = fileInfo.Length,
                        CreatedBy = jobData.User,
                        CreatedAt = DateTime.UtcNow,
                        ChangeDescription = "Imported via Bulk ZIP"
                    };

                    dbContext.Documents.Add(newDocument);
                    dbContext.DocumentVersions.Add(newVersion);

                    if (fileInfo.Extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        string extractedText = ExtractTextFromPdf(filePath);

                        if (!string.IsNullOrWhiteSpace(extractedText))
                        {
                            var searchDocument = new
                            {
                                Id = documentId,
                                Content = extractedText,
                                FileName = fileInfo.Name,
                                Owner = jobData.User
                            };

                            await _elasticClient.IndexAsync(searchDocument, idx => idx.Index("isdox-documents-index"), ct);
                        }
                    }

                    job.ProcessedFiles++;

                    if (job.ProcessedFiles % 10 == 0)
                    {
                        await dbContext.SaveChangesAsync(ct);
                    }
                }

                job.Status = "Completed";
                job.CompletedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(ct);

                await _s3Client.DeleteObjectAsync("documents", jobData.TempS3Key, ct);
            }
            finally
            {
                if (File.Exists(localZipPath)) File.Delete(localZipPath);
                if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true);
            }
        }

        private async Task MarkJobFailedAsync(Guid jobId, string errorMsg, CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IDmsDbContext>();

            var job = await dbContext.BulkImportJobs.FindAsync(new object[] { jobId }, ct);
            if (job != null)
            {
                job.Status = "Failed";
                job.ErrorMessage = errorMsg.Length > 500 ? errorMsg.Substring(0, 500) : errorMsg;
                job.CompletedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(ct);
            }
        }

        private string ExtractTextFromPdf(string filePath)
        {
            try
            {
                var textBuilder = new StringBuilder();
                using (var document = PdfDocument.Open(filePath))
                {
                    foreach (var page in document.GetPages())
                    {
                        textBuilder.Append(page.Text);
                        textBuilder.Append(" ");
                    }
                }
                return textBuilder.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"PdfPig failed to read text from {filePath}");
                return string.Empty;
            }
        }

        private class BulkImportMessage
        {
            public Guid JobId { get; set; }
            public string TempS3Key { get; set; } = string.Empty;
            public string User { get; set; } = string.Empty;
        }
    }
}
