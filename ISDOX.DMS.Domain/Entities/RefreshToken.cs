using System;
using System.Collections.Generic;
using System.Text;

namespace ISDOX.DMS.Domain.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiryTime { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
