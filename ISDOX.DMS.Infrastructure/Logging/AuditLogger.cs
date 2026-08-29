using ISDOX.DMS.Application.Interfaces;
using ISDOX.DMS.Domain.Entities;

namespace ISDOX.DMS.Infrastructure.Logging
{
    public class AuditLogger : IAuditLogger
    {
        private readonly IDmsDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public AuditLogger(IDmsDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task LogAsync(string actionType,
            Guid? entityId = null,
            string? entityName = null,
            string? folderPath = null,
            string status = "Success",
            string? overrideUserId = null,
            string? overrideUserEmail = null,
            CancellationToken ct = default)
        {
            var userId = overrideUserId ?? _currentUser.UserId ?? "System";
            var email = overrideUserEmail ?? _currentUser.UserEmail ?? "System";

            var log = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserEmail = email,
                ActionType = actionType,
                EntityId = entityId,
                EntityName = entityName,
                FolderPath = folderPath,
                Status = status,
                IpAddress = _currentUser.IpAddress,
                Device = _currentUser.Device,
                Browser = _currentUser.Browser,
                Timestamp = DateTime.Now
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync(ct);
        }
    }
}