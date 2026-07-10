using Amazon.S3;
using Amazon.S3.Model;
using Elastic.Clients.Elasticsearch;
using ISDOX.DMS.Application.Common.Behaviors;
using ISDOX.DMS.Application.Interfaces;
using ISDOX.DMS.Domain.Entities;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace ISDOX.DMS.Worker
{
    public class BulkImportBackgroundService : BackgroundService
    {
        private readonly IConnection _rabbitMqConnection;
        private readonly IAmazonS3 _s3Client;
        private readonly ElasticsearchClient _elasticClient;
        private readonly IServiceProvider _serviceProvider;
        private readonly IDocumentTextExtractor _textExtractor; 
        private readonly ILogger<BulkImportBackgroundService> _logger;

        public BulkImportBackgroundService(
            IConnection rabbitMqConnection,
            IAmazonS3 s3Client,
            ElasticsearchClient elasticClient,
            IServiceProvider serviceProvider,
            IDocumentTextExtractor textExtractor,
            ILogger<BulkImportBackgroundService> logger)
        {
            _rabbitMqConnection = rabbitMqConnection;
            _s3Client = s3Client;
            _elasticClient = elasticClient;
            _serviceProvider = serviceProvider;
            _textExtractor = textExtractor;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var channel = await _rabbitMqConnection.CreateChannelAsync(cancellationToken: stoppingToken);
            await channel.QueueDeclareAsync("bulk-import-queue", true, false, false, null, false, stoppingToken);
            await channel.BasicQosAsync(0, 1, false, stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var jobData = JsonSerializer.Deserialize<BulkImportMessage>(Encoding.UTF8.GetString(body));

                if (jobData == null)
                {
                    await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                    return;
                }

                try
                {
                    await ProcessZipAsync(jobData, stoppingToken);
                    await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                }
                catch (Exception ex)
                {
                    await MarkJobFailedAsync(jobData.JobId, ex.Message, stoppingToken);
                    await channel.BasicNackAsync(ea.DeliveryTag, false, false, stoppingToken);
                }
            };

            await channel.BasicConsumeAsync("bulk-import-queue", false, consumer, stoppingToken);
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
            var localCsvPath = Path.Combine(tempDir, $"{jobData.JobId}.csv");
            var extractPath = Path.Combine(tempDir, jobData.JobId.ToString());

            try
            {
                // Download Zip & CSV
                using (var response = await _s3Client.GetObjectAsync("isdox-documents", jobData.TempZipS3Key, ct))
                    await response.WriteResponseStreamToFileAsync(localZipPath, false, ct);

                var metadataMap = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                if (!string.IsNullOrEmpty(jobData.TempCsvS3Key))
                {
                    using (var response = await _s3Client.GetObjectAsync("isdox-documents", jobData.TempCsvS3Key, ct))
                        await response.WriteResponseStreamToFileAsync(localCsvPath, false, ct);

                    metadataMap = await ParseCsvMetadataAsync(localCsvPath);
                }

                ZipFile.ExtractToDirectory(localZipPath, extractPath);

                // FILTER: Only process files with extensions defined in our SupportedFileTypes array
                var files = Directory.GetFiles(extractPath, "*.*", SearchOption.AllDirectories)
                                     .Where(f => !new FileInfo(f).Attributes.HasFlag(FileAttributes.Hidden))
                                     .Where(f => SupportedFileTypes.IsSupported(f))
                                     .ToArray();

                job.TotalFiles = files.Length;
                await dbContext.SaveChangesAsync(ct);

                foreach (var filePath in files)
                {
                    var fileInfo = new FileInfo(filePath);
                    var extension = fileInfo.Extension.ToLowerInvariant();
                    var documentId = Guid.NewGuid();
                    var s3DestKey = $"documents/{jobData.User}/{documentId}{extension}";

                    using (var fs = fileInfo.OpenRead())
                    {
                        await _s3Client.PutObjectAsync(new PutObjectRequest
                        {
                            BucketName = "isdox-documents",
                            Key = s3DestKey,
                            InputStream = fs
                        }, ct);
                    }

                    metadataMap.TryGetValue(fileInfo.Name, out var customMetadataDict);

                    var newDocument = new Document
                    {
                        Id = documentId,
                        Name = fileInfo.Name,
                        Description = "Imported via Bulk ZIP",
                        FolderId = jobData.FolderId,
                        Owner = jobData.User,
                        CreatedAt = DateTime.UtcNow,
                        CustomMetadata = customMetadataDict ?? new Dictionary<string, string>()
                    };

                    var newVersion = new DocumentVersion
                    {
                        Id = Guid.NewGuid(),
                        DocumentId = documentId,
                        VersionNumber = 1,
                        StoragePath = s3DestKey,
                        FileExtension = extension,
                        FileSize = fileInfo.Length,
                        CreatedBy = jobData.User,
                        CreatedAt = DateTime.UtcNow
                    };

                    dbContext.Documents.Add(newDocument);
                    dbContext.DocumentVersions.Add(newVersion);

                    // UNIVERSAL TEXT EXTRACTION: Uses the dynamic strategy based on extension
                    string extractedText = _textExtractor.ExtractText(filePath, extension);

                    if (!string.IsNullOrWhiteSpace(extractedText))
                    {
                        var searchDocument = new
                        {
                            Id = documentId,
                            Content = extractedText,
                            FileName = fileInfo.Name,
                            Owner = jobData.User,
                            FolderId = jobData.FolderId
                        };
                        await _elasticClient.IndexAsync(searchDocument, idx => idx.Index("isdox-documents-index"), ct);
                    }

                    job.ProcessedFiles++;
                    if (job.ProcessedFiles % 10 == 0) await dbContext.SaveChangesAsync(ct);
                }

                job.Status = "Completed";
                job.CompletedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(ct);

                await _s3Client.DeleteObjectAsync("isdox-documents", jobData.TempZipS3Key, ct);
                if (!string.IsNullOrEmpty(jobData.TempCsvS3Key))
                    await _s3Client.DeleteObjectAsync("isdox-documents", jobData.TempCsvS3Key, ct);
            }
            finally
            {
                if (File.Exists(localZipPath)) File.Delete(localZipPath);
                if (File.Exists(localCsvPath)) File.Delete(localCsvPath);
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

        private async Task<Dictionary<string, Dictionary<string, string>>> ParseCsvMetadataAsync(string csvPath)
        {
            var metadata = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(csvPath)) return metadata;

            var lines = await File.ReadAllLinesAsync(csvPath);
            if (lines.Length < 2) return metadata;

            var headers = lines[0].Split(',').Select(h => h.Trim()).ToArray();
            int fileNameIndex = Array.FindIndex(headers, h =>
                h.Equals("FileName", StringComparison.OrdinalIgnoreCase) ||
                h.Equals("Name", StringComparison.OrdinalIgnoreCase));

            if (fileNameIndex == -1) return metadata;

            for (int i = 1; i < lines.Length; i++)
            {
                var values = lines[i].Split(',');
                if (values.Length != headers.Length) continue;

                var fileName = values[fileNameIndex].Trim();
                var rowData = new Dictionary<string, string>();

                for (int j = 0; j < headers.Length; j++)
                {
                    if (j == fileNameIndex) continue;
                    rowData[headers[j]] = values[j].Trim();
                }
                metadata[fileName] = rowData;
            }
            return metadata;
        }

        private class BulkImportMessage
        {
            public Guid JobId { get; set; }
            public string TempZipS3Key { get; set; } = string.Empty;
            public string? TempCsvS3Key { get; set; }
            public Guid? FolderId { get; set; }
            public string User { get; set; } = string.Empty;
        }
    }
}