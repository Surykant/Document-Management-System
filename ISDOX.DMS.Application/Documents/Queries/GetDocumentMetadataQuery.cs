using ISDOX.DMS.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Documents.Queries
{
    public record GetDocumentMetadataQuery(Guid Id) : IRequest<Dictionary<string, string>?>;

    public class GetDocumentMetadataHandler : IRequestHandler<GetDocumentMetadataQuery, Dictionary<string, string>?>
    {
        private readonly IDmsDbContext _context;
        public GetDocumentMetadataHandler(IDmsDbContext context) => _context = context;

        public async Task<Dictionary<string, string>?> Handle(GetDocumentMetadataQuery request, CancellationToken ct)
        {
            var doc = await _context.Documents
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == request.Id, ct);

            return doc?.CustomMetadata;
        }
    }
}
