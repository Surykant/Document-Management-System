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
            var query = _context.Documents.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.OwnerName))
                query = query.Where(d => d.Owner.ToLower().Contains(request.OwnerName.ToLower()));

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var search = request.SearchTerm.ToLower();
                query = query.Where(d => d.Name.ToLower().Contains(search) || d.Description.ToLower().Contains(search));
            }

            var totalCount = await query.CountAsync(ct);

            query = request.SortBy?.ToLower() switch
            {
                "name" => request.IsDescending ? query.OrderByDescending(d => d.Name) : query.OrderBy(d => d.Name),
                "owner" => request.IsDescending ? query.OrderByDescending(d => d.Owner) : query.OrderBy(d => d.Owner),
                _ => request.IsDescending ? query.OrderByDescending(d => d.CreatedAt) : query.OrderBy(d => d.CreatedAt),
            };

            var rawItems = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
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

            var items = rawItems.Select(d => new DocumentDto(
                d.Id, d.Name, d.Description, d.FolderId, d.Owner, d.CreatedAt, d.CustomMetadata,
                d.VersionNumber, d.FileExtension, d.StoragePath,
                d.CustomMetadata != null && d.CustomMetadata.ContainsKey("TemplateName") ? d.CustomMetadata["TemplateName"] : null,
                d.FolderName,
                FormatFileSize(d.FileSize) 
            )).ToList();

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
            var rawDoc = await _context.Documents
                .AsNoTracking()
                .Where(d => d.Id == request.Id)
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
                .FirstOrDefaultAsync(ct);

            if (rawDoc == null) return null;

            return new DocumentDto(
                rawDoc.Id, rawDoc.Name, rawDoc.Description, rawDoc.FolderId, rawDoc.Owner, rawDoc.CreatedAt, rawDoc.CustomMetadata,
                rawDoc.VersionNumber, rawDoc.FileExtension, rawDoc.StoragePath,
                rawDoc.CustomMetadata != null && rawDoc.CustomMetadata.ContainsKey("TemplateName") ? rawDoc.CustomMetadata["TemplateName"] : null,
                rawDoc.FolderName,
                FormatFileSize(rawDoc.FileSize) 
            );
        }

        public async Task<IEnumerable<DocumentDto>> Handle(GetDocumentsByFolderQuery request, CancellationToken ct)
        {
            return await _context.Documents
                .Where(d => d.FolderId == request.FolderId)
                .Include(d => d.Versions)
                .Select(d => MapToDto(d, _context))
                .ToListAsync(ct);
        }

        private static DocumentDto MapToDto(ISDOX.DMS.Domain.Entities.Document d, IDmsDbContext context)
        {
            var latest = d.Versions.OrderByDescending(v => v.VersionNumber).First();

            string? templateName = null;
            if (d.CustomMetadata != null && d.CustomMetadata.TryGetValue("TemplateName", out var tempName))
            {
                templateName = tempName;
            }

            return new DocumentDto(
                d.Id, d.Name, d.Description, d.FolderId, d.Owner, d.CreatedAt, d.CustomMetadata,
                latest.VersionNumber, latest.FileExtension, latest.StoragePath, templateName,
                null,
                FormatFileSize(latest.FileSize) 
            );
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