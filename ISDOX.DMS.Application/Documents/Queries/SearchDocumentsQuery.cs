using ISDOX.DMS.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Documents.Queries
{
    // The data shape returned to the Frontend
    public record DocumentSearchResult(
        Guid Id,
        string Name,
        string Description,
        Guid? FolderId,
        string? FolderName,
        string Owner,
        DateTime CreatedAt,
        string FileExtension,
        int VersionNumber);

    // The MediatR Request Command
    public record SearchDocumentsQuery(
        string Keyword,
        string? Owner,
        Guid? FolderId,        
        DateTime? FromDate,
        DateTime? ToDate,
        string? DocumentType
    ) : IRequest<List<DocumentSearchResult>>;

    // The Handler
    public class SearchDocumentsQueryHandler : IRequestHandler<SearchDocumentsQuery, List<DocumentSearchResult>>
    {
        private readonly ISearchService _searchService;
        private readonly IDmsDbContext _context;

        public SearchDocumentsQueryHandler(ISearchService searchService, IDmsDbContext context)
        {
            _searchService = searchService;
            _context = context;
        }

        public async Task<List<DocumentSearchResult>> Handle(SearchDocumentsQuery request, CancellationToken ct)
        {
            // 1. Get raw results from Elasticsearch
            var rawResults = await _searchService.SearchDocumentsAsync(
                request.Keyword, request.Owner, request.FolderId,
                request.FromDate, request.ToDate, request.DocumentType);

            if (!rawResults.Any())
            {
                return new List<DocumentSearchResult>();
            }

            // 2. Extract distinct Folder IDs from the search results
            var folderIds = rawResults
                .Where(r => r.FolderId.HasValue)
                .Select(r => r.FolderId.Value)
                .Distinct()
                .ToList();

            // 3. Fetch Folder Names from PostgreSQL efficiently
            var folderDictionary = new Dictionary<Guid, string>();
            if (folderIds.Any())
            {
                folderDictionary = await _context.Folders // Adjust 'Folders' if your DbSet is named differently
                    .Where(f => folderIds.Contains(f.Id))
                    .ToDictionaryAsync(f => f.Id, f => f.Name, ct);
            }

            // 4. Map results and stitch the FolderName in
            return rawResults.Select(d => new DocumentSearchResult(
                Id: d.Id,
                Name: d.Name ?? string.Empty,
                Description: d.Description ?? string.Empty,
                FolderId: d.FolderId,
                FolderName: d.FolderId.HasValue && folderDictionary.TryGetValue(d.FolderId.Value, out var folderName)
                            ? folderName
                            : null,
                Owner: d.Owner ?? "Unknown",
                CreatedAt: d.CreatedAt,
                FileExtension: d.FileExtension ?? string.Empty,
                VersionNumber: d.VersionNumber
            )).ToList();
        }
    }
}