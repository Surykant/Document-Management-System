using ISDOX.DMS.Application.Events;
using ISDOX.DMS.Application.Interfaces;
using ISDOX.DMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Documents.Commands
{
    public record CreateDocumentCommand(
        string Name,
        string Description,
        Guid FolderId,
        Stream FileStream,
        string FileName,
        long FileSizeInBytes,
        string CreatedBy,
        Dictionary<string, string>? Metadata) : IRequest<Guid>;

    public class CreateDocumentCommandHandler : IRequestHandler<CreateDocumentCommand, Guid>
    {
        private readonly IDmsDbContext _context;
        private readonly IStorageService _storage;
        private readonly IMessagePublisher _messagePublisher;

        public CreateDocumentCommandHandler(IDmsDbContext context, IStorageService storage, IMessagePublisher messagePublisher)
        {
            _context = context;
            _storage = storage;
            _messagePublisher = messagePublisher;
        }

        public async Task<Guid> Handle(CreateDocumentCommand request, CancellationToken ct)
        {
            var storagePath = await _storage.UploadFileAsync(request.FileStream, request.FileName, ct);

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
                    FileExtension = Path.GetExtension(request.FileName),
                    FileSize= request.FileSizeInBytes,
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
                    FileExtension = Path.GetExtension(request.FileName),
                    FileSize = request.FileSizeInBytes,
                    CreatedBy = request.CreatedBy,
                    ChangeDescription = "Initial Upload",
                    CreatedAt = DateTime.Now
                };

                newDocument.Versions.Add(firstVersion);
                _context.Documents.Add(newDocument);
                targetDocumentId = newDocument.Id;
            }

            await _context.SaveChangesAsync(ct);

            var uploadEvent = new DocumentUploadedEvent(
                DocumentId: targetDocumentId,
                FileName: request.FileName,
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