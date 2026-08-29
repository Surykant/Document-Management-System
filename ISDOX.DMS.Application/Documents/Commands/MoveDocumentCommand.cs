using ISDOX.DMS.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Documents.Commands
{
    public record MoveDocumentCommand(Guid DocumentId, Guid? TargetFolderId) : IRequest<bool>;

    public class MoveDocumentCommandHandler : IRequestHandler<MoveDocumentCommand, bool>
    {
        private readonly IDmsDbContext _context;
        private readonly IAuditLogger _auditLogger;

        public MoveDocumentCommandHandler(IDmsDbContext context, IAuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        public async Task<bool> Handle(MoveDocumentCommand request, CancellationToken ct)
        {
            string folderName = "Root";

            var document = await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == request.DocumentId, ct);

            if (document == null) return false;

            if (request.TargetFolderId.HasValue)
            {
                var targetFolder = await _context.Folders
                    .Where(f => f.Id == request.TargetFolderId.Value)
                    .Select(f => f.Name)
                    .FirstOrDefaultAsync(ct);

                if (targetFolder == null)
                    throw new Exception("Destination folder does not exist.");

                folderName = targetFolder; 
            }

            document.FolderId = request.TargetFolderId;

            await _context.SaveChangesAsync(ct);

            await _auditLogger.LogAsync(
                actionType: "Document Moved",
                entityId: document.Id,
                entityName: document.Name,
                folderPath: folderName,
                status: "Success",
                ct: ct
            );

            return true;
        }
    }
}
