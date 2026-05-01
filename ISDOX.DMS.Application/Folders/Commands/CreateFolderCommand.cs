using ISDOX.DMS.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ISDOX.DMS.Domain.Entities;

namespace ISDOX.DMS.Application.Folders.Commands
{
    public record CreateFolderCommand(string Name, Guid? ParentId, string CreatedBy) : IRequest<Guid>;

    public class CreateFolderCommandHandler : IRequestHandler<CreateFolderCommand, Guid>
    {
        private readonly IDmsDbContext _context;

        public CreateFolderCommandHandler(IDmsDbContext context)
        {
            _context = context;
        }

        public async Task<Guid> Handle(CreateFolderCommand request, CancellationToken ct)
        {
            var exists = await _context.Folders
                .AnyAsync(f => f.Name == request.Name && f.ParentId == request.ParentId, ct);

            if (exists)
                throw new Exception($"A folder named '{request.Name}' already exists in this location.");

            var folder = new Folder
            {
                Name = request.Name,
                ParentId = request.ParentId,
                CreatedBy = request.CreatedBy
            };

            _context.Folders.Add(folder);
            await _context.SaveChangesAsync(ct);

            return folder.Id;
        }
    }
}
