using ISDOX.DMS.Application.Interfaces;
using ISDOX.DMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Roles.Commands
{
    public record CreateRoleCommand(string Name, string Description) : IRequest<Guid>;

    public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, Guid>
    {
        private readonly IDmsDbContext _context;
        public CreateRoleCommandHandler(IDmsDbContext context) => _context = context;

        public async Task<Guid> Handle(CreateRoleCommand request, CancellationToken ct)
        {
            if (await _context.Roles.AnyAsync(r => r.Name == request.Name, ct))
                throw new Exception("Role already exists.");

            var role = new Role { Id = Guid.NewGuid(), Name = request.Name, Description = request.Description };
            _context.Roles.Add(role);
            await _context.SaveChangesAsync(ct);
            return role.Id;
        }
    }
}
