using ISDOX.DMS.Application.Common.Models; // Ensure this namespace matches your PagedResult location
using ISDOX.DMS.Application.Interfaces;
using ISDOX.DMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Users.Queries
{
    public record GetAllUsersQuery(
        string? SearchTerm = null,
        string? RoleName = null,
        int PageNumber = 1,
        int PageSize = 10
    ) : IRequest<PagedResult<UserDto>>;

    public record GetUserByIdQuery(Guid Id) : IRequest<UserDto?>;

    public record UserDto(
        Guid Id,
        string Username,
        string Name,
        string Email,
        string Department,
        string Role,
        List<string> Permissions,
        bool IsActive,
        DateTime CreatedAt,
        DateTime? DeletedAt);

    // 3. The Handler
    public class GetUserQueriesHandler :
        IRequestHandler<GetAllUsersQuery, PagedResult<UserDto>>, 
        IRequestHandler<GetUserByIdQuery, UserDto?>
    {
        private readonly IDmsDbContext _context;

        public GetUserQueriesHandler(IDmsDbContext context) => _context = context;

        public async Task<PagedResult<UserDto>> Handle(GetAllUsersQuery request, CancellationToken ct)
        {
            var query = _context.Users
                .Where(u => u.DeletedAt == null)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var search = request.SearchTerm.ToLower();
                query = query.Where(u =>
                    u.Name.ToLower().Contains(search) ||
                    u.Email.ToLower().Contains(search) ||
                    u.Username.ToLower().Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(request.RoleName))
            {
                query = query.Where(u => u.UserRoles.Any(ur => ur.Role != null && ur.Role.Name == request.RoleName));
            }

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(u => u.CreatedAt) 
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(u => new UserDto(
                    u.Id,
                    u.Username,
                    u.Name,
                    u.Email,
                    u.Department,
                    u.UserRoles.FirstOrDefault()!.Role!.Name ?? "User",
                    new List<string>(), 
                    u.IsActive,
                    u.CreatedAt,
                    u.DeletedAt))
                .ToListAsync(ct);

            return new PagedResult<UserDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }

        public async Task<UserDto?> Handle(GetUserByIdQuery request, CancellationToken ct)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == request.Id, ct);

            if (user == null) return null;

            var userRole = user.UserRoles.FirstOrDefault();
            var roleId = userRole?.RoleId;
            var roleName = userRole?.Role?.Name ?? "User";

            var permissions = new List<string>();

            if (roleId != null)
            {
                permissions = await _context.Set<RolePermission>()
                    .Where(rp => rp.RoleId == roleId)
                    .Join(
                        _context.Set<Permission>(),
                        rp => rp.PermissionId,
                        p => p.Id,
                        (rp, p) => p.Name
                    )
                    .Distinct()
                    .ToListAsync(ct);
            }

            return new UserDto(
                user.Id,
                user.Username,
                user.Name,
                user.Email,
                user.Department,
                roleName,
                permissions, 
                user.IsActive,
                user.CreatedAt,
                user.DeletedAt);
        }
    }
}