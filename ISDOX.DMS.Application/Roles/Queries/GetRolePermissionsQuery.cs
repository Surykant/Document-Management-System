using ISDOX.DMS.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Roles.Queries
{
    // 1. The DTO (Data Transfer Object) to send back to the frontend
    public record RolePermissionDto(
        Guid Id, // Or int/string depending on your Permission primary key type
        string Name // Optional, if your table has it
    );

    // 2. The MediatR Request
    public record GetRolePermissionsQuery(Guid RoleId) : IRequest<List<RolePermissionDto>>;

    // 3. The Handler
    public class GetRolePermissionsQueryHandler : IRequestHandler<GetRolePermissionsQuery, List<RolePermissionDto>>
    {
        private readonly IDmsDbContext _context;

        public GetRolePermissionsQueryHandler(IDmsDbContext context)
        {
            _context = context;
        }

        public async Task<List<RolePermissionDto>> Handle(GetRolePermissionsQuery request, CancellationToken ct)
        {
            // Fetch permissions explicitly linked to this RoleId.
            // Note: Adjust the DbSet names (e.g., RolePermissions, Permissions) based on your exact EF Core schema.
            var permissions = await _context.RolePermissions // Assuming a mapping table exists
                .AsNoTracking()
                .Where(rp => rp.RoleId == request.RoleId)
                .Select(rp => new RolePermissionDto(
                    rp.Permission.Id,
                    rp.Permission.Name
                ))
                .ToListAsync(ct);

            /* 
             * ALTERNATIVE: If you are using ASP.NET Core Identity's RoleClaims for permissions, 
             * your query would look like this instead:
             * 
             * var permissions = await _context.RoleClaims
             *    .AsNoTracking()
             *    .Where(rc => rc.RoleId == request.RoleId && rc.ClaimType == "Permission")
             *    .Select(rc => new RolePermissionDto(rc.Id, rc.ClaimValue, null))
             *    .ToListAsync(ct);
             */

            return permissions;
        }
    }
}