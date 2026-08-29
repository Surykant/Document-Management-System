using Amazon.S3;
using Amazon.S3.Model;
using ISDOX.DMS.Application.Interfaces;
using ISDOX.DMS.Domain.Entities;
using MediatR;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace ISDOX.DMS.Application.BulkImport
{
    public class StartBulkImportCommandHandler : IRequestHandler<StartBulkImportCommand, Guid>
    {
        private readonly IDmsDbContext _context;
        private readonly IAmazonS3 _s3Client;
        private readonly IConnection _rabbitMqConnection;
        private readonly IAuditLogger _auditLogger;

        public StartBulkImportCommandHandler(IDmsDbContext context, IAmazonS3 s3Client, IConnection rabbitMqConnection, IAuditLogger auditLogger)
        {
            _context = context;
            _s3Client = s3Client;
            _rabbitMqConnection = rabbitMqConnection;
            _auditLogger = auditLogger;
        }

        public async Task<Guid> Handle(StartBulkImportCommand request, CancellationToken ct)
        {
            var jobId = Guid.NewGuid();
            var tempZipS3Key = $"temp-imports/{jobId}.zip";
            string? tempCsvS3Key = null;

            using (var zipStream = request.ZipFile.OpenReadStream())
            {
                await _s3Client.PutObjectAsync(new PutObjectRequest
                {
                    BucketName = "documents",
                    Key = tempZipS3Key,
                    InputStream = zipStream
                }, ct);
            }

            if (request.CsvFile != null)
            {
                tempCsvS3Key = $"temp-imports/{jobId}.csv";
                using (var csvStream = request.CsvFile.OpenReadStream())
                {
                    await _s3Client.PutObjectAsync(new PutObjectRequest
                    {
                        BucketName = "documents",
                        Key = tempCsvS3Key,
                        InputStream = csvStream
                    }, ct);
                }
            }

            var job = new BulkImportJob
            {
                Id = jobId,
                CreatedBy = request.CurrentUser,
                OriginalFileName = request.ZipFile.FileName,
                TempStoragePath = tempZipS3Key,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };
            _context.BulkImportJobs.Add(job);
            await _context.SaveChangesAsync(ct);

            using var channel = await _rabbitMqConnection.CreateChannelAsync(cancellationToken: ct);

            await channel.QueueDeclareAsync(queue: "bulk-import-queue", durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: ct);

            var messagePayload = new
            {
                JobId = jobId,
                TempZipS3Key = tempZipS3Key,
                TempCsvS3Key = tempCsvS3Key,
                FolderId = request.FolderId,
                User = request.CurrentUser
            };

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(messagePayload));

            await channel.BasicPublishAsync(exchange: "", routingKey: "bulk-import-queue", body: body, cancellationToken: ct);

            await _auditLogger.LogAsync(
                actionType: "Bulk Import Initiated",
                entityId: jobId, 
                entityName: request.ZipFile.FileName, 
                status: "Success",
                ct: ct
            );

            return jobId;
        }
    }
}
