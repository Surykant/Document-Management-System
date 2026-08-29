using ISDOX.DMS.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Notifications.Commands
{
    public record MarkNotificationsAsReadCommand(string UserId, Guid? NotificationId = null) : IRequest<bool>;

    public class MarkNotificationsAsReadCommandHandler : IRequestHandler<MarkNotificationsAsReadCommand, bool>
    {
        private readonly IDmsDbContext _context;
        public MarkNotificationsAsReadCommandHandler(IDmsDbContext context) => _context = context;

        public async Task<bool> Handle(MarkNotificationsAsReadCommand request, CancellationToken ct)
        {
            var query = _context.Notifications.Where(n => n.UserId == request.UserId && !n.IsRead);

            if (request.NotificationId.HasValue)
            {
                query = query.Where(n => n.Id == request.NotificationId.Value);
            }

            var unreadNotifications = await query.ToListAsync(ct);

            foreach (var n in unreadNotifications)
            {
                n.IsRead = true;
            }

            if (unreadNotifications.Any())
            {
                await _context.SaveChangesAsync(ct);
            }

            return true;
        }
    }
}