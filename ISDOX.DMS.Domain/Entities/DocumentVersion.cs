using System;
using System.Collections.Generic;
using System.Text;

namespace ISDOX.DMS.Domain.Entities
{
    public class DocumentVersion
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid DocumentId { get; set; }
        public int VersionNumber { get; set; }
        public string StoragePath { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string ChangeDescription { get; set; } = string.Empty;
        public Document Document { get; set; } = null!;
    }
}
