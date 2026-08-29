using ISDOX.DMS.Application.Interfaces;
using ISDOX.DMS.Domain.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Documents.Queries
{
    // 1. The Request Payload
    public record AdvancedSearchDocumentsQuery(
        Guid TemplateId,
        Dictionary<string, string> Metadata
    ) : IRequest<List<DocumentDto>>;

    // 2. The Handler
    public class AdvancedSearchDocumentsQueryHandler : IRequestHandler<AdvancedSearchDocumentsQuery, List<DocumentDto>>
    {
        private readonly IDmsDbContext _context;

        public AdvancedSearchDocumentsQueryHandler(IDmsDbContext context)
        {
            _context = context;
        }

        public async Task<List<DocumentDto>> Handle(AdvancedSearchDocumentsQuery request, CancellationToken ct)
        {
            var templateName = await _context.MetadataTemplates
                .Where(m => m.Id == request.TemplateId)
                .Select(m => m.Name)
                .FirstOrDefaultAsync(ct);

            // 1. Start with the base query
            var query = _context.Documents.AsNoTracking().AsQueryable();

            // 2. Apply the Advanced JSON Search using FromSqlInterpolated
            if (request.Metadata != null && request.Metadata.Any())
            {
                // Serialize the dictionary payload into a flat JSON string
                var jsonSearch = System.Text.Json.JsonSerializer.Serialize(request.Metadata);

                // This executes PostgreSQL's native JSON containment operator (@>) safely.
                // It casts both sides to jsonb to ensure it works even if your DB column is mapped as text.
                query = _context.Documents
                    .FromSqlInterpolated($"SELECT * FROM \"Documents\" WHERE \"CustomMetadata\"::jsonb @> {jsonSearch}::jsonb")
                    .AsNoTracking();
            }

            // 3. EF Core will compose this Select directly on top of the raw SQL above!
            var rawItems = await query
                .Select(d => new
                {
                    d.Id,
                    d.Name,
                    d.Description,
                    d.FolderId,
                    d.Owner,
                    d.CreatedAt,
                    d.CustomMetadata,
                    VersionNumber = d.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault().VersionNumber,
                    FileExtension = d.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault().FileExtension,
                    StoragePath = d.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault().StoragePath,
                    FileSize = d.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault().FileSize,
                    FolderName = _context.Folders.Where(f => f.Id == d.FolderId).Select(f => f.Name).FirstOrDefault()
                })
                .ToListAsync(ct);

            // 4. Map to DTO in memory
            return rawItems.Select(d => new DocumentDto(
                d.Id, d.Name, d.Description, d.FolderId, d.Owner, d.CreatedAt, d.CustomMetadata,
                d.VersionNumber, d.FileExtension, d.StoragePath,
                templateName,
                d.FolderName,
                FormatFileSize(d.FileSize)
            )).ToList();
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes == 0) return "0 Bytes";

            string[] sizes = { "Bytes", "KB", "MB", "GB", "TB" };
            int order = 0;
            double len = bytes;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }

            return $"{len:0.##} {sizes[order]}";
        }
    }
}