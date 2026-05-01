using ISDOX.DMS.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Documents.Queries
{
    public record GetVersionHistoryQuery(Guid DocumentId) : IRequest<IEnumerable<VersionDto>>;
    public record VersionDto(Guid Id, int VersionNumber, string StoragePath, string FileExtension, string CreatedBy, string ChangeDescription, DateTime CreatedAt);

    public class GetVersionHistoryHandler : IRequestHandler<GetVersionHistoryQuery, IEnumerable<VersionDto>>
    {
        private readonly IDmsDbContext _context;
        public GetVersionHistoryHandler(IDmsDbContext context) => _context = context;

        public async Task<IEnumerable<VersionDto>> Handle(GetVersionHistoryQuery request, CancellationToken ct)
        {
            return await _context.DocumentVersions
                .Where(v => v.DocumentId == request.DocumentId)
                .OrderByDescending(v => v.VersionNumber)
                .Select(v => new VersionDto(v.Id, v.VersionNumber, v.StoragePath, v.FileExtension, v.CreatedBy, v.ChangeDescription, v.CreatedAt))
                .ToListAsync(ct);
        }
    }
}
