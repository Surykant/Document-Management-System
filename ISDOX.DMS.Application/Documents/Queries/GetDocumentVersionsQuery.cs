using Elastic.Clients.Elasticsearch;
using ISDOX.DMS.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Documents.Queries
{
    public record DocumentVersionHistoryDto(
        Guid Id,
        int VersionNumber,
        string ChangeDescription,
        string CreatedBy,
        string FileExtension,
        string DownloadApiLink);

    public record GetDocumentVersionsQuery(Guid DocumentId) : IRequest<List<DocumentVersionHistoryDto>>;

    public class GetDocumentVersionsQueryHandler : IRequestHandler<GetDocumentVersionsQuery, List<DocumentVersionHistoryDto>>
    {
        private readonly IDmsDbContext _context;

        public GetDocumentVersionsQueryHandler(IDmsDbContext context)
        {
            _context = context;
        }

        public async Task<List<DocumentVersionHistoryDto>> Handle(GetDocumentVersionsQuery request, CancellationToken ct)
        {
            var versions = await _context.DocumentVersions
                .Where(v => v.DocumentId == request.DocumentId)
                .OrderByDescending(v => v.VersionNumber)
                .ToListAsync(ct);

            if (!versions.Any())
                throw new FileNotFoundException("No versions found for this document.");

            return versions.Select(v => new DocumentVersionHistoryDto(
                Id: v.Id,
                VersionNumber: v.VersionNumber,
                ChangeDescription: v.ChangeDescription,
                CreatedBy: v.CreatedBy,
                FileExtension: v.FileExtension,
                DownloadApiLink: $"/api/documents/{request.DocumentId}/versions/{v.VersionNumber}/download"
            )).ToList();
        }
    }
}
