namespace ISDOX.DMS.Domain.Models.Search
{
    public class DocumentSearchModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
        public Guid? FolderId { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Tags { get; set; } = new();

        public Dictionary<string, string> Metadata { get; set; } = new();

        public string FileExtension { get; set; } = string.Empty;
        public int VersionNumber { get; set; }
    }
}
