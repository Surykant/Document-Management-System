using ISDOX.DMS.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Users.Queries
{
    public record GetAllUsersQuery() : IRequest<IEnumerable<UserDto>>;

    public record GetUserByIdQuery(Guid Id) : IRequest<UserDto?>;

    public record UserDto(
        Guid Id,
        string Username,
        string Email,
        string Department,
        bool IsActive,
        DateTime CreatedAt,
        DateTime? DeletedAt);

    public class GetUserQueriesHandler :
        IRequestHandler<GetAllUsersQuery, IEnumerable<UserDto>>,
        IRequestHandler<GetUserByIdQuery, UserDto?>
    {
        private readonly IDmsDbContext _context;

        public GetUserQueriesHandler(IDmsDbContext context) => _context = context;

        public async Task<IEnumerable<UserDto>> Handle(GetAllUsersQuery request, CancellationToken ct)
        {
            return await _context.Users
                .Where(u => u.DeletedAt == null) // Filter out soft-deleted users
                .Select(u => new UserDto(
                    u.Id, u.Username, u.Email, u.Department, u.IsActive, u.CreatedAt, u.DeletedAt))
                .ToListAsync(ct);
        }

        public async Task<UserDto?> Handle(GetUserByIdQuery request, CancellationToken ct)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.Id, ct);

            if (user == null) return null;

            return new UserDto(
                user.Id, user.Username, user.Email, user.Department, user.IsActive, user.CreatedAt, user.DeletedAt);
        }
    }
}
