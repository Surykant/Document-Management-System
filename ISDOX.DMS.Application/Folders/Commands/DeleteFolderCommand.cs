using ISDOX.DMS.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Folders.Commands
{
    public record DeleteFolderCommand(Guid Id) : IRequest<bool>;

    public class DeleteFolderCommandHandler : IRequestHandler<DeleteFolderCommand, bool>
    {
        private readonly IDmsDbContext _context;
        public DeleteFolderCommandHandler(IDmsDbContext context) => _context = context;

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
            return true;
        }
    }
}
