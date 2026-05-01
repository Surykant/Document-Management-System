namespace ISDOX.DMS.Domain.Entities
{
    public class MetadataTemplate
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty; 
        public string Description { get; set; } = string.Empty;

        public List<string> AllowedFields { get; set; } = new();

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
