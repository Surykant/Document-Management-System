namespace ISDOX.DMS.Domain.Entities
{
    public class AuditLog
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;

        public string ActionType { get; set; } = string.Empty;

        public Guid? EntityId { get; set; } 
        public string? EntityName { get; set; } 
        public string? FolderPath { get; set; } 

        public string Status { get; set; } = "Success"; 

        public string? IpAddress { get; set; }
        public string? Device { get; set; }
        public string? Browser { get; set; }

        public DateTime Timestamp { get; set; }
    }
}