using System;
using System.Collections.Generic;
using System.Text;

namespace ISDOX.DMS.Worker
{
    public class DocumentIndexModel
    {
        public Guid DocumentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime IndexedAt { get; set; } = DateTime.Now;
        public string Owner { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
