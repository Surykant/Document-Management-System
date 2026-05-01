using ISDOX.DMS.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Documents.Commands
{
    public record MoveDocumentCommand(Guid DocumentId, Guid? TargetFolderId) : IRequest<bool>;

    public class MoveDocumentCommandHandler : IRequestHandler<MoveDocumentCommand, bool>
    {
        private readonly IDmsDbContext _context;

        public MoveDocumentCommandHandler(IDmsDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(MoveDocumentCommand request, CancellationToken ct)
        {
            var document = await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == request.DocumentId, ct);

            if (document == null) return false;

            if (request.TargetFolderId.HasValue)
            {
                var folderExists = await _context.Folders
                    .AnyAsync(f => f.Id == request.TargetFolderId.Value, ct);

                if (!folderExists)
                    throw new Exception("Destination folder does not exist.");
            }

            document.FolderId = request.TargetFolderId;

            await _context.SaveChangesAsync(ct);
            return true;
        }
    }
}
