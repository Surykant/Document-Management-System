using ISDOX.DMS.Application.Interfaces;
using ISDOX.DMS.Domain.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Documents.Queries
{
    public record GetAllDocumentsQuery() : IRequest<IEnumerable<DocumentDto>>;
    public record GetDocumentByIdQuery(Guid Id) : IRequest<DocumentDto?>;
    public record GetDocumentsByFolderQuery(Guid FolderId) : IRequest<IEnumerable<DocumentDto>>;

    public class GetDocumentQueriesHandler :
        IRequestHandler<GetAllDocumentsQuery, IEnumerable<DocumentDto>>,
        IRequestHandler<GetDocumentByIdQuery, DocumentDto?>,
        IRequestHandler<GetDocumentsByFolderQuery, IEnumerable<DocumentDto>>
    {
        private readonly IDmsDbContext _context;

        public GetDocumentQueriesHandler(IDmsDbContext context) => _context = context;

        public async Task<IEnumerable<DocumentDto>> Handle(GetAllDocumentsQuery request, CancellationToken ct)
        {
            return await _context.Documents
                .Include(d => d.Versions)
                .Select(d => MapToDto(d))
                .ToListAsync(ct);
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
            return new DocumentDto(
                d.Id, d.Name, d.Description, d.FolderId, d.Owner, d.CreatedAt,
                d.CustomMetadata, latest.VersionNumber, latest.FileExtension, latest.StoragePath);
        }
    }
}
