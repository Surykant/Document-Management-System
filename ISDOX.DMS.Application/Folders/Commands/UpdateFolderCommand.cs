using ISDOX.DMS.Application.Interfaces;
using MediatR;

namespace ISDOX.DMS.Application.Folders.Commands
{
    public record UpdateFolderCommand(Guid Id, string NewName) : IRequest<bool>;

    public class UpdateFolderCommandHandler : IRequestHandler<UpdateFolderCommand, bool>
    {
        private readonly IDmsDbContext _context;
        private readonly IAuditLogger _auditLogger;

        public UpdateFolderCommandHandler(IDmsDbContext context, IAuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        public async Task<bool> Handle(UpdateFolderCommand request, CancellationToken ct)
        {
            var folder = await _context.Folders.FindAsync(new object[] { request.Id }, ct);
            if (folder == null) return false;

            folder.Name = request.NewName;
            await _context.SaveChangesAsync(ct);

            await _auditLogger.LogAsync(
                        actionType: "Folder Updated",
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
