using ISDOX.DMS.Application.Interfaces;
using ISDOX.DMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Roles.Commands
{
    public record AssignRoleCommand(Guid UserId, string RoleName) : IRequest<bool>;

    public class AssignRoleCommandHandler : IRequestHandler<AssignRoleCommand, bool>
    {
        private readonly IDmsDbContext _context;
        public AssignRoleCommandHandler(IDmsDbContext context) => _context = context;

        public async Task<bool> Handle(AssignRoleCommand request, CancellationToken ct)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == request.RoleName, ct);
            if (role == null)
            {
                role = new Role { Id = Guid.NewGuid(), Name = request.RoleName };
                _context.Roles.Add(role);
            }

            var alreadyAssigned = await _context.UserRoles
                .AnyAsync(ur => ur.UserId == request.UserId && ur.RoleId == role.Id, ct);

            if (!alreadyAssigned)
            {
                _context.UserRoles.Add(new UserRole { UserId = request.UserId, RoleId = role.Id });
                await _context.SaveChangesAsync(ct);
            }
            return true;
        }
    }
}
