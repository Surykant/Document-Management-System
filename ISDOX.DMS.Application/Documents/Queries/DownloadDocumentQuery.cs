using ISDOX.DMS.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Documents.Queries
{
    public record DocumentDownloadDto(Stream Content, string ContentType, string DownloadFileName);

    public record DownloadDocumentQuery(Guid DocumentId) : IRequest<DocumentDownloadDto>;

    public class DownloadDocumentQueryHandler : IRequestHandler<DownloadDocumentQuery, DocumentDownloadDto>
    {
        private readonly IDmsDbContext _context;
        private readonly IStorageService _storage;
        private readonly IAuditLogger _auditLogger;


        public DownloadDocumentQueryHandler(IDmsDbContext context, IStorageService storage, IAuditLogger auditLogger)
        {
            _context = context;
            _storage = storage;
            _auditLogger = auditLogger;
        }

        public async Task<DocumentDownloadDto> Handle(DownloadDocumentQuery request, CancellationToken ct)
        {
            var document = await _context.Documents.FindAsync(new object[] { request.DocumentId }, ct);
            if (document == null)
                throw new FileNotFoundException("Document metadata not found.");

            var latestVersion = await _context.DocumentVersions
                .Where(v => v.DocumentId == request.DocumentId)
                .OrderByDescending(v => v.VersionNumber)
                .FirstOrDefaultAsync(ct);

            if (latestVersion == null)
                throw new FileNotFoundException("No file versions exist for this document.");

            var stream = await _storage.DownloadFileAsync(latestVersion.StoragePath, ct);

            await _auditLogger.LogAsync(
                actionType: "Document Downloaded",
                entityId: document.Id,
                entityName: document.Name,
                // folderPath: document.,
                status: "Success",
                ct: ct
            );

            return new DocumentDownloadDto(
                Content: stream,
                ContentType: "application/octet-stream",
                DownloadFileName: document.Name + latestVersion.FileExtension
            );
        }
    }
}