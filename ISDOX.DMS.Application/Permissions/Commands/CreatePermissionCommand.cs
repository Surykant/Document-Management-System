using ISDOX.DMS.Application.Interfaces;
using ISDOX.DMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Permissions.Commands
{
    public record CreatePermissionCommand(string Name, string Description) : IRequest<Guid>;

    public class CreatePermissionCommandHandler : IRequestHandler<CreatePermissionCommand, Guid>
    {
        private readonly IDmsDbContext _context;
        public CreatePermissionCommandHandler(IDmsDbContext context) => _context = context;

        public async Task<Guid> Handle(CreatePermissionCommand request, CancellationToken ct)
        {
            if (await _context.Permissions.AnyAsync(p => p.Name == request.Name, ct))
                throw new Exception("Permission already exists.");

            var permission = new Permission { Id = Guid.NewGuid(), Name = request.Name, Description = request.Description };
            _context.Permissions.Add(permission);
            await _context.SaveChangesAsync(ct);
            return permission.Id;
        }
    }
}
