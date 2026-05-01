using ISDOX.DMS.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ISDOX.DMS.Application.Dashboard.Queries.GetDashboardStats
{
    public record GetDashboardStatsQuery(string CurrentUser, bool IsAdmin) : IRequest<DashboardStatsDto>;

    public class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
    {
        private readonly IDmsDbContext _context;
        private readonly IConfiguration _config;

        public GetDashboardStatsQueryHandler(IDmsDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken ct)
        {
            var today = DateTime.UtcNow.Date;
            var sevenDaysAgo = today.AddDays(-7);

            // 1. Setup the base queries (Do not execute them yet!)
            var documentsQuery = _context.Documents.AsQueryable();
            var versionsQuery = _context.DocumentVersions.AsQueryable();

            // 2. Apply the Privacy Lock ONLY if they are a regular user
            if (!request.IsAdmin)
            {
                documentsQuery = documentsQuery.Where(d => d.Owner == request.CurrentUser);
                versionsQuery = versionsQuery.Where(v => v.CreatedBy == request.CurrentUser);
            }

            // 3. NOW execute the math against the database
            var totalDocs = await documentsQuery.CountAsync(ct);

            var docsToday = await documentsQuery
                .Where(d => d.CreatedAt >= today)
                .CountAsync(ct);

            var storageUsed = await versionsQuery.SumAsync(v => v.FileSize, ct);

            var quotaString = _config["DmsSettings:GlobalStorageQuotaBytes"];
            var globalQuota = string.IsNullOrEmpty(quotaString) ? 107374182400 : long.Parse(quotaString);

            // 4. Get Recent Activities (using the filtered versionsQuery)` 
            var recentActivities = await versionsQuery
                .Include(v => v.Document)
                .OrderByDescending(v => v.CreatedAt)
                .Take(5)
                .Select(v => new RecentActivityDto
                {
                    DocumentId = v.DocumentId,
                    DocumentName = v.Document.Name,
                    Action = v.VersionNumber == 1 ? "Uploaded" : "Updated",
                    User = v.CreatedBy,
                    Timestamp = v.CreatedAt
                })
                .ToListAsync(ct);

            // 5. Calculate User Activity Chart
            var recentUploads = await documentsQuery
                .Where(d => d.CreatedAt >= sevenDaysAgo)
                .GroupBy(d => d.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var userActivityChart = new List<UserActivityDto>();
            for (int i = 6; i >= 0; i--)
            {
                var targetDate = today.AddDays(-i);
                userActivityChart.Add(new UserActivityDto
                {
                    Date = targetDate.ToString("MMM dd"),
                    UploadCount = recentUploads.FirstOrDefault(u => u.Date == targetDate)?.Count ?? 0
                });
            }

            return new DashboardStatsDto
            {
                TotalDocuments = totalDocs,
                DocumentsUploadedToday = docsToday,
                StorageUsedBytes = storageUsed,
                StorageQuotaBytes = globalQuota,
                RecentActivities = recentActivities,
                UserActivity = userActivityChart
            };
        }
    }
}
