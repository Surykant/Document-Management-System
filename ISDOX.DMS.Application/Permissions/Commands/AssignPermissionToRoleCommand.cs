using ISDOX.DMS.Application.Interfaces;
using ISDOX.DMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace ISDOX.DMS.Application.Permissions.Commands
{
    public record AssignPermissionsToRoleCommand(Guid RoleId, List<Guid> PermissionIds) : IRequest<bool>;

    public class AssignPermissionsToRoleCommandHandler : IRequestHandler<AssignPermissionsToRoleCommand, bool>
    {
        private readonly IDmsDbContext _context;

        public AssignPermissionsToRoleCommandHandler(IDmsDbContext context) => _context = context;

        public async Task<bool> Handle(AssignPermissionsToRoleCommand request, CancellationToken ct)
        {
            if (request.PermissionIds == null || !request.PermissionIds.Any())
                return true; 

            var existingPermissionIds = await _context.RolePermissions
                .Where(rp => rp.RoleId == request.RoleId && request.PermissionIds.Contains(rp.PermissionId))
                .Select(rp => rp.PermissionId)
                .ToListAsync(ct);

            var permissionsToAdd = request.PermissionIds
                .Except(existingPermissionIds)
                .Select(permissionId => new RolePermission
                {
                    RoleId = request.RoleId,
                    PermissionId = permissionId
                })
                .ToList();

            if (permissionsToAdd.Any())
            {
                _context.RolePermissions.AddRange(permissionsToAdd);
                await _context.SaveChangesAsync(ct);
            }

            return true;
        }
    }
}
