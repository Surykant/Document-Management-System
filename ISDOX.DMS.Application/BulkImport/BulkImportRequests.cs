using MediatR;
using Microsoft.AspNetCore.Http;

namespace ISDOX.DMS.Application.BulkImport
{
    public record StartBulkImportCommand(
        IFormFile ZipFile,
        IFormFile? CsvFile,
        Guid? FolderId,
        string CurrentUser) : IRequest<Guid>;

    public record GetBulkImportStatusQuery(Guid JobId) : IRequest<BulkImportStatusDto?>;

    public record BulkImportStatusDto(
        Guid Id,
        string Status,
        int TotalFiles,
        int ProcessedFiles,
        string? ErrorMessage);
}
