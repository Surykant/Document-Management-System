using ISDOX.DMS.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Documents.Commands
{
    public record UpdateDocumentMetadataCommand(
        Guid Id,
        string Name,
        string Description,
        Dictionary<string, string>? Metadata) : IRequest<bool>;

    public class UpdateDocumentMetadataCommandHandler : IRequestHandler<UpdateDocumentMetadataCommand, bool>
    {
        private readonly IDmsDbContext _context;
        private readonly IAuditLogger _auditLogger;


        public UpdateDocumentMetadataCommandHandler(IDmsDbContext context, IAuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        public async Task<bool> Handle(UpdateDocumentMetadataCommand request, CancellationToken ct)
        {
            var document = await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == request.Id, ct);

            if (document == null) return false;

            document.Name = request.Name;
            document.Description = request.Description;

            if (request.Metadata != null)
            {
                document.CustomMetadata = request.Metadata;
            }

            await _context.SaveChangesAsync(ct);

            await _auditLogger.LogAsync(
                    actionType: "Document Metadata Updated",
                    entityId: document.Id,
                    entityName: document.Name,
                    // folderPath: document.,
                    status: "Success",
                    ct: ct
                );
            return true;
        }
    }
}
