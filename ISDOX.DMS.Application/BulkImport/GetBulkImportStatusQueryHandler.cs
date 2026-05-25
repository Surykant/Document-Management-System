using ISDOX.DMS.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.BulkImport
{
    public class GetBulkImportStatusQueryHandler : IRequestHandler<GetBulkImportStatusQuery, BulkImportStatusDto?>
    {
        private readonly IDmsDbContext _context;

        public GetBulkImportStatusQueryHandler(IDmsDbContext context)
        {
            _context = context;
        }

        public async Task<BulkImportStatusDto?> Handle(GetBulkImportStatusQuery request, CancellationToken ct)
        {
            var job = await _context.BulkImportJobs
                .AsNoTracking()
                .FirstOrDefaultAsync(j => j.Id == request.JobId, ct);

            if (job == null)
                return null;

            return new BulkImportStatusDto(
                job.Id,
                job.Status,
                job.TotalFiles,
                job.ProcessedFiles,
                job.ErrorMessage
            );
        }
    }
}
