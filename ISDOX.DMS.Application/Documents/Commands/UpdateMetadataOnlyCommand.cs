using ISDOX.DMS.Application.Interfaces;
using MediatR;

namespace ISDOX.DMS.Application.Documents.Commands
{
    public record UpdateMetadataOnlyCommand(Guid Id, Dictionary<string, string> Metadata) : IRequest<bool>;

    public class UpdateMetadataOnlyHandler : IRequestHandler<UpdateMetadataOnlyCommand, bool>
    {
        private readonly IDmsDbContext _context;
        public UpdateMetadataOnlyHandler(IDmsDbContext context) => _context = context;

        public async Task<bool> Handle(UpdateMetadataOnlyCommand request, CancellationToken ct)
        {
            var doc = await _context.Documents.FindAsync(new object[] { request.Id }, ct);
            if (doc == null) return false;

            doc.CustomMetadata = request.Metadata;

            await _context.SaveChangesAsync(ct);
            return true;
        }
    }
}
