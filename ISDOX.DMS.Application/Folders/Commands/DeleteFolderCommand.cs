using ISDOX.DMS.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Folders.Commands
{
    public record DeleteFolderCommand(Guid Id) : IRequest<bool>;

    public class DeleteFolderCommandHandler : IRequestHandler<DeleteFolderCommand, bool>
    {
        private readonly IDmsDbContext _context;
        private readonly IAuditLogger _auditLogger;

        public DeleteFolderCommandHandler(IDmsDbContext context, IAuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        public async Task<bool> Handle(DeleteFolderCommand request, CancellationToken ct)
        {
            var folder = await _context.Folders
                .Include(f => f.SubFolders)
                .Include(f => f.Documents)
                .FirstOrDefaultAsync(f => f.Id == request.Id, ct);

            if (folder == null) return false;

            if (folder.SubFolders.Any() || folder.Documents.Any())
            {
                throw new Exception("Cannot delete folder because it is not empty. Move or delete contents first.");
            }

            _context.Folders.Remove(folder);
            await _context.SaveChangesAsync(ct);

            await _auditLogger.LogAsync(
                        actionType: "Folder Deleted",
                        entityId: folder.Id,
                        entityName: folder.Name,
                        // folderPath: folder.,
                        status: "Success",
                        ct: ct
                    );
            return true;
        }
    }
}
