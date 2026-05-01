using ISDOX.DMS.Application.Interfaces;
using ISDOX.DMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace ISDOX.DMS.Application.Permissions.Commands
{
    public record AssignPermissionToRoleCommand(Guid RoleId, Guid PermissionId) : IRequest<bool>;

    public class AssignPermissionToRoleCommandHandler : IRequestHandler<AssignPermissionToRoleCommand, bool>
    {
        private readonly IDmsDbContext _context;
        public AssignPermissionToRoleCommandHandler(IDmsDbContext context) => _context = context;

        public async Task<bool> Handle(AssignPermissionToRoleCommand request, CancellationToken ct)
        {
            var exists = await _context.RolePermissions
                .AnyAsync(rp => rp.RoleId == request.RoleId && rp.PermissionId == request.PermissionId, ct);

            if (!exists)
            {
                _context.RolePermissions.Add(new RolePermission
                {
                    RoleId = request.RoleId,
                    PermissionId = request.PermissionId
                });
                await _context.SaveChangesAsync(ct);
            }
            return true;
        }
    }
}
