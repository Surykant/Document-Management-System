using ISDOX.DMS.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Documents.Queries
{
    public record DownloadSpecificVersionQuery(Guid DocumentId, int VersionNumber) : IRequest<DocumentDownloadDto>;

    public class DownloadSpecificVersionQueryHandler : IRequestHandler<DownloadSpecificVersionQuery, DocumentDownloadDto>
    {
        private readonly IDmsDbContext _context;
        private readonly IStorageService _storage;

        public DownloadSpecificVersionQueryHandler(IDmsDbContext context, IStorageService storage)
        {
            _context = context;
            _storage = storage;
        }

        public async Task<DocumentDownloadDto> Handle(DownloadSpecificVersionQuery request, CancellationToken ct)
        {
            var document = await _context.Documents.FindAsync(new object[] { request.DocumentId }, ct);
            if (document == null)
                throw new FileNotFoundException("Document metadata not found.");

            var specificVersion = await _context.DocumentVersions
                .FirstOrDefaultAsync(v => v.DocumentId == request.DocumentId && v.VersionNumber == request.VersionNumber, ct);

            if (specificVersion == null)
                throw new FileNotFoundException($"Version {request.VersionNumber} does not exist for this document.");

            var stream = await _storage.DownloadFileAsync(specificVersion.StoragePath, ct);

            var versionedFileName = $"{Path.GetFileNameWithoutExtension(document.Name)}_v{specificVersion.VersionNumber}{specificVersion.FileExtension}";

            return new DocumentDownloadDto(
                Content: stream,
                ContentType: "application/octet-stream",
                DownloadFileName: versionedFileName
            );
        }
    }
}
