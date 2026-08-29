using ISDOX.DMS.Application.Common.Models;
using ISDOX.DMS.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.AuditLogs.Queries
{
    public record AuditLogDto(Guid Id, string UserEmail, string ActionType, string? EntityName, string? FolderPath, string Status, DateTime Timestamp);

    public record GetAuditLogsQuery(int PageNumber = 1, int PageSize = 10, string? UserId = null, string? ActionType = null) : IRequest<PagedResult<AuditLogDto>>;

    public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PagedResult<AuditLogDto>>
    {
        private readonly IDmsDbContext _context;
        public GetAuditLogsQueryHandler(IDmsDbContext context) => _context = context;

        public async Task<PagedResult<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken ct)
        {
            var query = _context.AuditLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.UserId)) query = query.Where(a => a.UserId == request.UserId);
            if (!string.IsNullOrWhiteSpace(request.ActionType) && request.ActionType != "All Activity Types")
                query = query.Where(a => a.ActionType == request.ActionType);

            var totalCount = await query.CountAsync(ct);

            var items = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(a => new AuditLogDto(a.Id, a.UserEmail, a.ActionType, a.EntityName, a.FolderPath, a.Status, a.Timestamp))
                .ToListAsync(ct);

            return new PagedResult<AuditLogDto> { Items = items, TotalCount = totalCount, PageNumber = request.PageNumber, PageSize = request.PageSize };
        }
    }
}