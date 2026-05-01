using ISDOX.DMS.Application.Events;
using ISDOX.DMS.Application.Interfaces;
using ISDOX.DMS.Domain.Models.Search;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Infrastructure.Messaging
{
    public class DocumentUploadedConsumer : IMessageConsumer<DocumentUploadedEvent>
    {
        private readonly ISearchService _searchService;
        private readonly IDmsDbContext _context;

        public DocumentUploadedConsumer(ISearchService searchService, IDmsDbContext context)
        {
            _searchService = searchService;
            _context = context;
        }

        public async Task HandleAsync(DocumentUploadedEvent @event, CancellationToken ct)
        {
            var doc = await _context.Documents
                .Include(d => d.Versions)
                .FirstOrDefaultAsync(d => d.Id == @event.DocumentId, ct);

            if (doc == null) return;

            var latestVersion = doc.Versions.OrderByDescending(v => v.VersionNumber).First();

            var searchModel = new DocumentSearchModel
            {
                Id = doc.Id,
                Name = doc.Name,
                Description = doc.Description,
                Owner = doc.Owner,
                FolderId = doc.FolderId,
                CreatedAt = doc.CreatedAt,
                Metadata = doc.CustomMetadata,
                FileExtension = latestVersion.FileExtension,
                VersionNumber = latestVersion.VersionNumber
            };

            await _searchService.IndexDocumentAsync(searchModel);
        }
    }
}
