using Amazon.S3;
using Amazon.S3.Model;
using Elastic.Clients.Elasticsearch;
using ISDOX.DMS.Application.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using UglyToad.PdfPig;

namespace ISDOX.DMS.Worker;

public class DocumentProcessorWorker : BackgroundService
{
    private readonly IConnection _connection;
    private readonly ILogger<DocumentProcessorWorker> _logger;
    private readonly ElasticsearchClient _elasticClient;
    private readonly IAmazonS3 _amazonS3;

    public DocumentProcessorWorker(
        IConnection connection,
        ILogger<DocumentProcessorWorker> logger,
        ElasticsearchClient elasticClient,
        IAmazonS3 amazonS3)
    {
        _connection = connection;
        _logger = logger;
        _elasticClient = elasticClient;
        _amazonS3 = amazonS3;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Document Processor Worker is waking up and connecting to RabbitMQ...");

        // =================================================================================
        // FOR UPLOADS
        // =================================================================================
        var uploadChannel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
        var uploadQueueName = nameof(DocumentUploadedEvent);

        await uploadChannel.QueueDeclareAsync(queue: uploadQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: stoppingToken);

        var uploadConsumer = new AsyncEventingBasicConsumer(uploadChannel);

        uploadConsumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var documentEvent = JsonSerializer.Deserialize<DocumentUploadedEvent>(message);

                if (documentEvent == null) throw new Exception("Failed to deserialize message.");

                _logger.LogInformation("NEW UPLOAD JOB RECEIVED! Processing Document ID: {DocumentId}, File: {FileName}",
                    documentEvent.DocumentId, documentEvent.FileName);

                var extractedText = new StringBuilder();

                if (Path.GetExtension(documentEvent.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var getRequest = new GetObjectRequest
                        {
                            BucketName = "documents",
                            Key = documentEvent.StoragePath
                        };

                        using var s3Response = await _amazonS3.GetObjectAsync(getRequest, stoppingToken);

                        using var memoryStream = new MemoryStream();
                        await s3Response.ResponseStream.CopyToAsync(memoryStream, stoppingToken);
                        memoryStream.Position = 0;

                        using (var pdfDocument = PdfDocument.Open(memoryStream))
                        {
                            foreach (var page in pdfDocument.GetPages())
                            {
                                extractedText.AppendLine(page.Text);
                            }
                        }

                        _logger.LogInformation("Successfully extracted {CharacterCount} characters of text from the PDF!", extractedText.Length);

                        var snippet = extractedText.Length > 100 ? extractedText.ToString().Substring(0, 100) : extractedText.ToString();
                        _logger.LogInformation("Snippet: {Snippet}...", snippet.Replace("\n", " ").Replace("\r", ""));

                        var indexDocument = new DocumentIndexModel
                        {
                            DocumentId = documentEvent.DocumentId,
                            FileName = documentEvent.FileName,
                            Content = extractedText.ToString(),
                            Metadata = documentEvent.Metadata ?? new Dictionary<string, string>(), 
                            Owner = documentEvent.Owner,
                            CreatedAt = DateTime.Now
                        };

                        var indexResponse = await _elasticClient.IndexAsync(indexDocument, idx => idx.Index("documents"), stoppingToken);

                        if (indexResponse.IsValidResponse)
                        {
                            _logger.LogInformation("Successfully indexed document {DocumentId} into Elasticsearch!", documentEvent.DocumentId);
                        }
                        else
                        {
                            _logger.LogError("Failed to index document in Elasticsearch: {Error}", indexResponse.DebugInformation);
                        }
                    }
                    catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _logger.LogWarning("File not found in S3 bucket at {Path}. Cannot process.", documentEvent.StoragePath);
                        await uploadChannel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                        return;
                    }
                }
                else
                {
                    _logger.LogInformation("File is not a PDF. Skipping text extraction.");
                }

                _logger.LogInformation("Successfully finished processing Document ID: {DocumentId}", documentEvent.DocumentId);

                await uploadChannel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing upload message.");
                await uploadChannel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
            }
        };

        await uploadChannel.BasicConsumeAsync(queue: uploadQueueName, autoAck: false, consumer: uploadConsumer, cancellationToken: stoppingToken);


        // =================================================================================
        // FOR DELETIONS
        // =================================================================================
        var deleteChannel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
        var deleteQueueName = nameof(DocumentDeletedEvent);

        await deleteChannel.QueueDeclareAsync(queue: deleteQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: stoppingToken);

        var deleteConsumer = new AsyncEventingBasicConsumer(deleteChannel);

        deleteConsumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var documentDeletedEvent = JsonSerializer.Deserialize<DocumentDeletedEvent>(message);

                if (documentDeletedEvent == null) throw new Exception("Failed to deserialize delete message.");

                _logger.LogInformation("DELETION JOB RECEIVED! Purging Document ID: {DocumentId}", documentDeletedEvent.DocumentId);

                var deleteResponse = await _elasticClient.DeleteAsync<DocumentIndexModel>(
                    documentDeletedEvent.DocumentId,
                    idx => idx.Index("documents"),
                    stoppingToken);

                if (deleteResponse.IsValidResponse)
                {
                    _logger.LogInformation("Successfully purged Document {Id} from Elasticsearch.", documentDeletedEvent.DocumentId);
                }
                else
                {
                    _logger.LogWarning("Elasticsearch Delete Note: {Message}", deleteResponse.DebugInformation);
                }

                await deleteChannel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing delete message.");
                await deleteChannel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
            }
        };

        await deleteChannel.BasicConsumeAsync(queue: deleteQueueName, autoAck: false, consumer: deleteConsumer, cancellationToken: stoppingToken);

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}