using ISDOX.DMS.Application.Events;
using ISDOX.DMS.Application.Interfaces;
using ISDOX.DMS.Domain.Entities;
using ISDOX.DMS.Domain.Models.Search;
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
        private readonly ISearchService _searchService;
        private readonly IDocumentTextExtractor _textExtractor;
        private readonly IAuditLogger _auditLogger;

        public CreateDocumentCommandHandler(
            IDmsDbContext context,
            IStorageService storage,
            IMessagePublisher messagePublisher,
            ISearchService searchService, 
            IDocumentTextExtractor textExtractor,
            IAuditLogger auditLogger)
        {
            _context = context;
            _storage = storage;
            _messagePublisher = messagePublisher;
            _searchService = searchService;
            _textExtractor = textExtractor;
            _auditLogger = auditLogger;
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
            int currentVersionNumber;
            DateTime documentCreatedAt;

            if (existingDocument != null)
            {
                currentVersionNumber = existingDocument.Versions.Any()
                    ? existingDocument.Versions.Max(v => v.VersionNumber) + 1
                    : 1;

                documentCreatedAt = existingDocument.CreatedAt;

                if (request.Metadata != null)
                {
                    existingDocument.CustomMetadata = request.Metadata;
                }

                var newVersion = new DocumentVersion
                {
                    DocumentId = existingDocument.Id,
                    VersionNumber = currentVersionNumber,
                    StoragePath = storagePath,
                    FileExtension = extension,
                    FileSize = fileSize,
                    CreatedBy = request.CreatedBy,
                    ChangeDescription = string.IsNullOrWhiteSpace(request.Description)
                        ? $"Updated to Version {currentVersionNumber}"
                        : request.Description
                };

                _context.DocumentVersions.Add(newVersion);
                targetDocumentId = existingDocument.Id;

                await _auditLogger.LogAsync(
                actionType: "Document Version Updated",
                entityId: existingDocument.Id,
                entityName: fileName, 
                folderPath: storagePath,
                status: "Success",
                ct: ct
            );
            }
            else
            {
                currentVersionNumber = 1;
                documentCreatedAt = DateTime.Now;

                var newDocument = new Document
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    Description = request.Description,
                    FolderId = request.FolderId,
                    Owner = request.CreatedBy,
                    CreatedAt = documentCreatedAt,
                    CustomMetadata = request.Metadata ?? new Dictionary<string, string>()
                };

                var firstVersion = new DocumentVersion
                {
                    Id = Guid.NewGuid(),
                    DocumentId = newDocument.Id,
                    VersionNumber = currentVersionNumber,
                    StoragePath = storagePath,
                    FileExtension = extension,
                    FileSize = fileSize,
                    CreatedBy = request.CreatedBy,
                    ChangeDescription = "Initial Upload",
                    CreatedAt = documentCreatedAt
                };

                newDocument.Versions.Add(firstVersion);
                _context.Documents.Add(newDocument);
                targetDocumentId = newDocument.Id;

                await _auditLogger.LogAsync(
               actionType: "Document Created",
               entityId: newDocument.Id,
               entityName: request.Name,
               folderPath: storagePath,
               status: "Success",
               ct: ct
           );
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

                var searchModel = new DocumentSearchModel
                {
                    Id = targetDocumentId,
                    Name = request.Name,
                    Description = request.Description,
                    FolderId = request.FolderId,
                    Owner = request.CreatedBy,
                    CreatedAt = documentCreatedAt,
                    FileExtension = extension, 
                    VersionNumber = currentVersionNumber,
                };

                await _searchService.IndexDocumentAsync(searchModel);
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
                CreatedAt : documentCreatedAt,
                Metadata: request.Metadata ?? new Dictionary<string, string>()
            );

            await _messagePublisher.PublishAsync(uploadEvent, ct);

            return targetDocumentId;
        }
    }
}