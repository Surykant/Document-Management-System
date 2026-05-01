namespace ISDOX.DMS.Application.Dashboard.Queries.GetDashboardStats
{
    public class DashboardStatsDto
    {
        public int TotalDocuments { get; set; }
        public int DocumentsUploadedToday { get; set; }
        public long StorageUsedBytes { get; set; }
        public long StorageQuotaBytes { get; set; }
        public List<RecentActivityDto> RecentActivities { get; set; } = new();
        public List<UserActivityDto> UserActivity { get; set; } = new();
    }

    public class RecentActivityDto
    {
        public Guid DocumentId { get; set; }
        public string DocumentName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class UserActivityDto
    {
        public string Date { get; set; } = string.Empty;
        public int UploadCount { get; set; }
    }
}
