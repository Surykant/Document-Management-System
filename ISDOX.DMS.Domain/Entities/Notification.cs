namespace ISDOX.DMS.Domain.Entities
{
    public class Notification
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty; 
        public string Type { get; set; } = string.Empty; 
        public string Message { get; set; } = string.Empty; 

        public Guid? DocumentId { get; set; }
        public string? DocumentName { get; set; }
        public string? FolderPath { get; set; }

        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
