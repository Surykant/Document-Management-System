namespace ISDOX.DMS.Domain.Entities
{
    public class Document
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid? FolderId { get; set; }
        public Folder? Folder { get; set; }
        public string Owner { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, string> CustomMetadata { get; set; } = new();
        public List<DocumentVersion> Versions { get; set; } = new();
    }
}
