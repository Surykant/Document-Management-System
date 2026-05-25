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

        public StartBulkImportCommandHandler(IDmsDbContext context, IAmazonS3 s3Client, IConnection rabbitMqConnection)
        {
            _context = context;
            _s3Client = s3Client;
            _rabbitMqConnection = rabbitMqConnection;
        }

        public async Task<Guid> Handle(StartBulkImportCommand request, CancellationToken ct)
        {
            var jobId = Guid.NewGuid();
            var tempS3Key = $"temp-imports/{jobId}.zip";

            using var stream = request.File.OpenReadStream();
            var putRequest = new PutObjectRequest
            {
                BucketName = "documents",
                Key = tempS3Key,
                InputStream = stream
            };
            await _s3Client.PutObjectAsync(putRequest, ct);

            var job = new BulkImportJob
            {
                Id = jobId,
                CreatedBy = request.CurrentUser,
                OriginalFileName = request.File.FileName,
                TempStoragePath = tempS3Key,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };
            _context.BulkImportJobs.Add(job);
            await _context.SaveChangesAsync(ct);

            using var channel = await _rabbitMqConnection.CreateChannelAsync(cancellationToken: ct);

            await channel.QueueDeclareAsync(
                queue: "bulk-import-queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: ct);

            var message = JsonSerializer.Serialize(new { JobId = jobId, TempS3Key = tempS3Key, User = request.CurrentUser });
            var body = Encoding.UTF8.GetBytes(message);

            await channel.BasicPublishAsync(
                exchange: "",
                routingKey: "bulk-import-queue",
                body: body,
                cancellationToken: ct);

            return jobId;
        }
    }
}
