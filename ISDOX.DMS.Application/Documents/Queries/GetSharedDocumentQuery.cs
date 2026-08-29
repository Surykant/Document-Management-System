using ISDOX.DMS.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Documents.Queries
{
    public record SharedDocumentDto(
        Guid DocumentId,
        string FileName,
        long FileSize,
        bool RequiresPassword,
        string? DownloadUrl 
    );

    public record GetSharedDocumentQuery(string Token) : IRequest<SharedDocumentDto?>;

    public class GetSharedDocumentQueryHandler : IRequestHandler<GetSharedDocumentQuery, SharedDocumentDto?>
    {
        private readonly IDmsDbContext _context;

        public GetSharedDocumentQueryHandler(IDmsDbContext context) => _context = context;

        public async Task<SharedDocumentDto?> Handle(GetSharedDocumentQuery request, CancellationToken ct)
        {
            var share = await _context.DocumentShares
                .Include(s => s.Document)
                .ThenInclude(d => d.Versions)
                .FirstOrDefaultAsync(s => s.Token == request.Token, ct);

            if (share == null || share.IsRevoked)
                return null;

            if (share.ExpiryDate.HasValue && share.ExpiryDate.Value < DateTime.UtcNow)
                throw new UnauthorizedAccessException("This share link has expired.");

            var latestVersion = share.Document.Versions.OrderByDescending(v => v.VersionNumber).First();

            return new SharedDocumentDto(
                DocumentId: share.DocumentId,
                FileName: share.Document.Name + latestVersion.FileExtension,
                FileSize: latestVersion.FileSize,
                RequiresPassword: share.IsPasswordProtected,
                DownloadUrl: share.IsPasswordProtected ? null : latestVersion.StoragePath 
            );
        }
    }
}