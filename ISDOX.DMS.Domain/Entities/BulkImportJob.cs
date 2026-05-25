using System;
using System.Collections.Generic;
using System.Text;

namespace ISDOX.DMS.Domain.Entities
{
    public class BulkImportJob
    {
        public Guid Id { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string TempStoragePath { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; 
        public int TotalFiles { get; set; } = 0;
        public int ProcessedFiles { get; set; } = 0;
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? CompletedAt { get; set; }
    }
}
