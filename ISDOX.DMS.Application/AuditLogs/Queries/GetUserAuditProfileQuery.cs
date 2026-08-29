using ISDOX.DMS.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.AuditLogs.Queries
{
    public record UserAuditProfileDto(
        string Email,
        string? LastLoginIp,
        string? LastLoginDevice,
        string? LastLoginBrowser,
        DateTime? LastLoginTime,
        List<RecentActivityDto> RecentActivities
    );

    public record RecentActivityDto(string ActionType, string? EntityName, string? FolderPath, DateTime Timestamp);

    public record GetUserAuditProfileQuery(string UserId) : IRequest<UserAuditProfileDto?>;

    public class GetUserAuditProfileQueryHandler : IRequestHandler<GetUserAuditProfileQuery, UserAuditProfileDto?>
    {
        private readonly IDmsDbContext _context;
        public GetUserAuditProfileQueryHandler(IDmsDbContext context) => _context = context;

        public async Task<UserAuditProfileDto?> Handle(GetUserAuditProfileQuery request, CancellationToken ct)
        {
            var lastLogin = await _context.AuditLogs
                .AsNoTracking()
                .Where(a => a.UserId == request.UserId && a.ActionType == "Login")
                .OrderByDescending(a => a.Timestamp)
                .FirstOrDefaultAsync(ct);

            var recentFeed = await _context.AuditLogs
                .AsNoTracking()
                .Where(a => a.UserId == request.UserId)
                .OrderByDescending(a => a.Timestamp)
                .Take(5)
                .Select(a => new RecentActivityDto(a.ActionType, a.EntityName, a.FolderPath, a.Timestamp))
                .ToListAsync(ct);

            if (lastLogin == null && !recentFeed.Any()) return null;

            return new UserAuditProfileDto(
                Email: lastLogin?.UserEmail ?? recentFeed.FirstOrDefault()?.EntityName ?? "Unknown",
                LastLoginIp: lastLogin?.IpAddress,
                LastLoginDevice: lastLogin?.Device,
                LastLoginBrowser: lastLogin?.Browser,
                LastLoginTime: lastLogin?.Timestamp,
                RecentActivities: recentFeed
            );
        }
    }
}