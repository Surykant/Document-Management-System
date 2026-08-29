using ISDOX.DMS.Application.Interfaces;
using ISDOX.DMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Documents.Commands
{
    public record RestoreVersionCommand(Guid DocumentId, Guid VersionId, string RequestedBy) : IRequest<bool>;

    public class RestoreVersionCommandHandler : IRequestHandler<RestoreVersionCommand, bool>
    {
        private readonly IDmsDbContext _context;
        private readonly IAuditLogger _auditLogger;

        public RestoreVersionCommandHandler(IDmsDbContext context, IAuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        public async Task<bool> Handle(RestoreVersionCommand request, CancellationToken ct)
        {
            var versions = await _context.DocumentVersions
                .Where(v => v.DocumentId == request.DocumentId)
                .ToListAsync(ct);

            var targetVersion = versions.FirstOrDefault(v => v.Id == request.VersionId);
            if (targetVersion == null) return false;

            var newVersion = new DocumentVersion
            {
                Id = Guid.NewGuid(),
                DocumentId = request.DocumentId,
                VersionNumber = versions.Max(v => v.VersionNumber) + 1,
                StoragePath = targetVersion.StoragePath,
                FileExtension = targetVersion.FileExtension,
                CreatedBy = request.RequestedBy,
                ChangeDescription = $"Restored from Version {targetVersion.VersionNumber}",
                CreatedAt = DateTime.Now 
            };

            _context.DocumentVersions.Add(newVersion);
            await _context.SaveChangesAsync(ct);

            await _auditLogger.LogAsync(
                        actionType: "Document version restored.",
                        entityId: newVersion.DocumentId,
                        //entityName: targetVersion.,
                         folderPath: targetVersion.StoragePath,
                        status: "Success",
                        ct: ct
                    );
            return true;
        }
    }
}
