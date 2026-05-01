namespace ISDOX.DMS.Domain.Entities
{
    public class Folder
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;

        public Guid? ParentId { get; set; }
        public Folder? Parent { get; set; }

        public ICollection<Folder> SubFolders { get; set; } = new List<Folder>();
        public ICollection<Document> Documents { get; set; } = new List<Document>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
    }
}
