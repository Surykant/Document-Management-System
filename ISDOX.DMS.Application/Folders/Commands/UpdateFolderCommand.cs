using ISDOX.DMS.Application.Interfaces;
using MediatR;

namespace ISDOX.DMS.Application.Folders.Commands
{
    public record UpdateFolderCommand(Guid Id, string NewName) : IRequest<bool>;

    public class UpdateFolderCommandHandler : IRequestHandler<UpdateFolderCommand, bool>
    {
        private readonly IDmsDbContext _context;
        public UpdateFolderCommandHandler(IDmsDbContext context) => _context = context;

        public async Task<bool> Handle(UpdateFolderCommand request, CancellationToken ct)
        {
            var folder = await _context.Folders.FindAsync(new object[] { request.Id }, ct);
            if (folder == null) return false;

            folder.Name = request.NewName;
            await _context.SaveChangesAsync(ct);
            return true;
        }
    }
}
