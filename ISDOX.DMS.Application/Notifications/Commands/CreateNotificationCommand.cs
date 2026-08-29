using ISDOX.DMS.Application.Interfaces;
using ISDOX.DMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Notifications.Commands
{
    public record CreateNotificationCommand(
        string? TargetUserId, 
        string? TargetRole,   
        string Type,          
        string Message,
        Guid? DocumentId = null,
        string? DocumentName = null,
        string? FolderPath = null
    ) : IRequest<bool>;

    public class CreateNotificationCommandHandler : IRequestHandler<CreateNotificationCommand, bool>
    {
        private readonly IDmsDbContext _context;

        public CreateNotificationCommandHandler(IDmsDbContext context) => _context = context;

        public async Task<bool> Handle(CreateNotificationCommand request, CancellationToken ct)
        {
            var notifications = new List<Notification>();
            var now = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(request.TargetUserId))
            {
                notifications.Add(BuildNotification(request.TargetUserId, request, now));
            }
            else if (!string.IsNullOrWhiteSpace(request.TargetRole))
            {
                var adminUserIds = await _context.UserRoles
                    .Where(ur => ur.Role.Name == request.TargetRole)
                    .Select(ur => ur.UserId)
                    .ToListAsync(ct);

                foreach (var adminId in adminUserIds)
                {
                    notifications.Add(BuildNotification(adminId.ToString(), request, now));
                }
            }

            if (notifications.Any())
            {
                _context.Notifications.AddRange(notifications);
                await _context.SaveChangesAsync(ct);
            }

            return true;
        }

        private static Notification BuildNotification(string userId, CreateNotificationCommand req, DateTime time)
        {
            return new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = req.Type,
                Message = req.Message,
                DocumentId = req.DocumentId,
                DocumentName = req.DocumentName,
                FolderPath = req.FolderPath,
                IsRead = false,
                CreatedAt = time
            };
        }
    }
}