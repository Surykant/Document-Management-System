using ISDOX.DMS.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Roles.Queries
{
    public record GetAllRolesQuery() : IRequest<IEnumerable<RoleDto>>;

    public record RoleDto(Guid Id, string Name, string Description);

    public class GetRolesQueryHandler : IRequestHandler<GetAllRolesQuery, IEnumerable<RoleDto>>
    {
        private readonly IDmsDbContext _context;
        public GetRolesQueryHandler(IDmsDbContext context) => _context = context;

        public async Task<IEnumerable<RoleDto>> Handle(GetAllRolesQuery request, CancellationToken ct)
        {
            return await _context.Roles
                .Select(r => new RoleDto(r.Id, r.Name, r.Description))
                .ToListAsync(ct);
        }
    }
}
