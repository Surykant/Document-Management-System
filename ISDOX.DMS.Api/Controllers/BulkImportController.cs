using ISDOX.DMS.Application.BulkImport;
using ISDOX.DMS.Domain.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ISDOX.DMS.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BulkImportController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BulkImportController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> UploadZip([FromForm] BulkImportRequestDto request)
        {
            if (request.ZipFile == null || !request.ZipFile.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                return BadRequest("A valid ZIP file is required for the document payload.");

            if (request.CsvFile != null && !request.CsvFile.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Metadata file must be a CSV.");

            var currentUser = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";

            var command = new StartBulkImportCommand(
                request.ZipFile,
                request.CsvFile,
                request.FolderId,
                currentUser);

            var jobId = await _mediator.Send(command);

            return Accepted($"/api/bulk-import/status/{jobId}", new { JobId = jobId });
        }

        [HttpGet("status/{jobId:guid}")]
        public async Task<IActionResult> GetStatus(Guid jobId)
        {
            var query = new GetBulkImportStatusQuery(jobId);
            var result = await _mediator.Send(query);

            if (result == null) return NotFound("Job not found.");

            return Ok(result);
        }
    }
}
