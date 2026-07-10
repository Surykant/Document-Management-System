using Elastic.Clients.Elasticsearch;
using ISDOX.DMS.Application.Events;
using ISDOX.DMS.Application.Interfaces;
using ISDOX.DMS.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Documents.Commands
{
    public record CreateDocumentCommand(
        string Name,
        string Description,
        Guid FolderId,
        IFormFile File, 
        string CreatedBy,
        Dictionary<string, string>? Metadata) : IRequest<Guid>;

    public class CreateDocumentCommandHandler : IRequestHandler<CreateDocumentCommand, Guid>
    {
        private readonly IDmsDbContext _context;
        private readonly IStorageService _storage;
        private readonly IMessagePublisher _messagePublisher;
        private readonly ElasticsearchClient _elasticClient;        
        private readonly IDocumentTextExtractor _textExtractor;      

        public CreateDocumentCommandHandler(
            IDmsDbContext context,
            IStorageService storage,
            IMessagePublisher messagePublisher,
            ElasticsearchClient elasticClient,
            IDocumentTextExtractor textExtractor)
        {
            _context = context;
            _storage = storage;
            _messagePublisher = messagePublisher;
            _elasticClient = elasticClient;
            _textExtractor = textExtractor;
        }

        public async Task<Guid> Handle(CreateDocumentCommand request, CancellationToken ct)
        {
            var fileName = request.File.FileName;
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var fileSize = request.File.Length;

            using var stream = request.File.OpenReadStream();
            var storagePath = await _storage.UploadFileAsync(stream, fileName, ct);

            var existingDocument = await _context.Documents
                .Include(d => d.Versions)
                .FirstOrDefaultAsync(d => d.Name == request.Name && d.FolderId == request.FolderId, ct);

            Guid targetDocumentId;

            if (existingDocument != null)
            {
                var nextVersionNumber = existingDocument.Versions.Any()
                    ? existingDocument.Versions.Max(v => v.VersionNumber) + 1
                    : 1;

                if (request.Metadata != null)
                {
                    existingDocument.CustomMetadata = request.Metadata;
                }

                var newVersion = new DocumentVersion
                {
                    DocumentId = existingDocument.Id,
                    VersionNumber = nextVersionNumber,
                    StoragePath = storagePath,
                    FileExtension = extension,
                    FileSize = fileSize,
                    CreatedBy = request.CreatedBy,
                    ChangeDescription = string.IsNullOrWhiteSpace(request.Description)
                        ? $"Updated to Version {nextVersionNumber}"
                        : request.Description
                };

                _context.DocumentVersions.Add(newVersion);
                targetDocumentId = existingDocument.Id;
            }
            else
            {
                var newDocument = new Document
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    Description = request.Description,
                    FolderId = request.FolderId,
                    Owner = request.CreatedBy,
                    CreatedAt = DateTime.Now,
                    CustomMetadata = request.Metadata ?? new Dictionary<string, string>()
                };

                var firstVersion = new DocumentVersion
                {
                    Id = Guid.NewGuid(),
                    DocumentId = newDocument.Id,
                    VersionNumber = 1,
                    StoragePath = storagePath,
                    FileExtension = extension,
                    FileSize = fileSize,
                    CreatedBy = request.CreatedBy,
                    ChangeDescription = "Initial Upload",
                    CreatedAt = DateTime.Now
                };

                newDocument.Versions.Add(firstVersion);
                _context.Documents.Add(newDocument);
                targetDocumentId = newDocument.Id;
            }

            await _context.SaveChangesAsync(ct);

            var tempPath = Path.GetTempFileName();
            try
            {
                using (var fs = new FileStream(tempPath, FileMode.Create))
                {
                    await request.File.CopyToAsync(fs, ct);
                }

                string extractedText = _textExtractor.ExtractText(tempPath, extension);

                if (!string.IsNullOrWhiteSpace(extractedText))
                {
                    var searchDocument = new
                    {
                        Id = targetDocumentId,
                        Content = extractedText,
                        FileName = fileName,
                        Owner = request.CreatedBy,
                        FolderId = request.FolderId
                    };

                    await _elasticClient.IndexAsync(searchDocument, idx => idx.Index("isdox-documents-index"), ct);
                }
            }
            finally
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }

            var uploadEvent = new DocumentUploadedEvent(
                DocumentId: targetDocumentId,
                FileName: fileName,
                StoragePath: storagePath,
                Owner: request.CreatedBy,
                CreatedAt: DateTime.Now,
                Metadata: request.Metadata ?? new Dictionary<string, string>()
            );

            await _messagePublisher.PublishAsync(uploadEvent, ct);

            return targetDocumentId;
        }
    }
}