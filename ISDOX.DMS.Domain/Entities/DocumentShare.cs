namespace ISDOX.DMS.Domain.Entities
{
    public class DocumentShare
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public string Token { get; set; } = string.Empty; 
        public bool IsPasswordProtected { get; set; }
        public string? PasswordHash { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsRevoked { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;

        public Document Document { get; set; } = null!;
    }
}
