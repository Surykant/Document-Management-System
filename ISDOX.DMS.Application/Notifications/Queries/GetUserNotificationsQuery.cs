using ISDOX.DMS.Application.Common.Models;
using ISDOX.DMS.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Notifications.Queries
{
    public record NotificationDto(
        Guid Id, string Type, string Message, Guid? DocumentId,
        string? DocumentName, string? FolderPath, bool IsRead, DateTime CreatedAt
    );

    public record GetUserNotificationsQuery(
        string UserId, string? TypeFilter = null, int PageNumber = 1, int PageSize = 10
    ) : IRequest<PagedResult<NotificationDto>>;

    public class GetUserNotificationsQueryHandler : IRequestHandler<GetUserNotificationsQuery, PagedResult<NotificationDto>>
    {
        private readonly IDmsDbContext _context;
        public GetUserNotificationsQueryHandler(IDmsDbContext context) => _context = context;

        public async Task<PagedResult<NotificationDto>> Handle(GetUserNotificationsQuery request, CancellationToken ct)
        {
            var query = _context.Notifications.AsNoTracking().Where(n => n.UserId == request.UserId);

            if (!string.IsNullOrWhiteSpace(request.TypeFilter) && request.TypeFilter.ToLower() != "all")
            {
                var filter = request.TypeFilter.ToLower();
                query = query.Where(n => n.Type.ToLower() == filter);
            }

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(n => new NotificationDto(
                    n.Id, n.Type, n.Message, n.DocumentId, n.DocumentName, n.FolderPath, n.IsRead, n.CreatedAt
                ))
                .ToListAsync(ct);

            return new PagedResult<NotificationDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }
    }
}