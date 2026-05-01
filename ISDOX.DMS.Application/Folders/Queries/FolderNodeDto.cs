using System;
using System.Collections.Generic;
using System.Text;

namespace ISDOX.DMS.Application.Folders.Queries
{
    public class FolderNodeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid? ParentId { get; set; }

        public List<FolderNodeDto> Children { get; set; } = new();

        public int DocumentCount { get; set; }
    }
}
