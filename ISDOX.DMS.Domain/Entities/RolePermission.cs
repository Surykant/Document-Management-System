using System;
using System.Collections.Generic;
using System.Text;

namespace ISDOX.DMS.Domain.Entities
{
    public class RolePermission
    {
        public Guid RoleId { get; set; }
        public Role Role { get; set; } = null!;
        public Guid PermissionId { get; set; }
        public Permission Permission { get; set; } = null!;
    }
}
