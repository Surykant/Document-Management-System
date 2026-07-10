using ISDOX.DMS.Application.Common.Models;
using ISDOX.DMS.Application.Interfaces;
using ISDOX.DMS.Domain.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Documents.Queries
{
    public record GetAllDocumentsQuery(
         int PageNumber = 1,
         int PageSize = 10,
         string? SearchTerm = null,
         string? OwnerName = null, 
         string? SortBy = "CreatedAt", 
         bool IsDescending = true
     ) : IRequest<PagedResult<DocumentDto>>;
    public record GetDocumentByIdQuery(Guid Id) : IRequest<DocumentDto?>;
    public record GetDocumentsByFolderQuery(Guid FolderId) : IRequest<IEnumerable<DocumentDto>>;

    public class GetDocumentQueriesHandler :
        IRequestHandler<GetAllDocumentsQuery, PagedResult<DocumentDto>>,
        IRequestHandler<GetDocumentByIdQuery, DocumentDto?>,
        IRequestHandler<GetDocumentsByFolderQuery, IEnumerable<DocumentDto>>
    {
        private readonly IDmsDbContext _context;

        public GetDocumentQueriesHandler(IDmsDbContext context) => _context = context;

        public async Task<PagedResult<DocumentDto>> Handle(GetAllDocumentsQuery request, CancellationToken ct)
        {
            var query = _context.Documents
                .AsNoTracking()
                .AsQueryable();

            // 2. Apply Filters
            if (!string.IsNullOrWhiteSpace(request.OwnerName))
            {
                query = query.Where(d => d.Owner.ToLower().Contains(request.OwnerName.ToLower()));
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var search = request.SearchTerm.ToLower();
                query = query.Where(d =>
                    d.Name.ToLower().Contains(search) ||
                    d.Description.ToLower().Contains(search));
            }

            var totalCount = await query.CountAsync(ct);

            query = request.SortBy?.ToLower() switch
            {
                "name" => request.IsDescending ? query.OrderByDescending(d => d.Name) : query.OrderBy(d => d.Name),
                "owner" => request.IsDescending ? query.OrderByDescending(d => d.Owner) : query.OrderBy(d => d.Owner),
                _ => request.IsDescending ? query.OrderByDescending(d => d.CreatedAt) : query.OrderBy(d => d.CreatedAt),
            };

            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(d => new DocumentDto(
                    d.Id,
                    d.Name,
                    d.Description,
                    d.FolderId,
                    d.Owner,
                    d.CreatedAt,
                    d.CustomMetadata,
                    d.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault().VersionNumber,
                    d.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault().FileExtension,
                    d.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault().StoragePath,
                    d.CustomMetadata != null && d.CustomMetadata.ContainsKey("TemplateName")
                        ? d.CustomMetadata["TemplateName"]
                        : null
                ))
                .ToListAsync(ct);

            // 6. Return the PagedResult
            return new PagedResult<DocumentDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }

        public async Task<DocumentDto?> Handle(GetDocumentByIdQuery request, CancellationToken ct)
        {
            var doc = await _context.Documents
                .Include(d => d.Versions)
                .FirstOrDefaultAsync(d => d.Id == request.Id, ct);

            return doc != null ? MapToDto(doc) : null;
        }

        public async Task<IEnumerable<DocumentDto>> Handle(GetDocumentsByFolderQuery request, CancellationToken ct)
        {
            return await _context.Documents
                .Where(d => d.FolderId == request.FolderId)
                .Include(d => d.Versions)
                .Select(d => MapToDto(d))
                .ToListAsync(ct);
        }

        private static DocumentDto MapToDto(ISDOX.DMS.Domain.Entities.Document d)
        {
            var latest = d.Versions.OrderByDescending(v => v.VersionNumber).First();

            string? templateName = null;
            if (d.CustomMetadata != null && d.CustomMetadata.TryGetValue("TemplateName", out var tempName))
            {
                templateName = tempName;
            }

            return new DocumentDto(
                d.Id,
                d.Name,
                d.Description,
                d.FolderId,
                d.Owner,
                d.CreatedAt,
                d.CustomMetadata,
                latest.VersionNumber,
                latest.FileExtension,
                latest.StoragePath,
                templateName); 
        }
    }
}
