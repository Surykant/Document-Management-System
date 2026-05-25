using ISDOX.DMS.Application.BulkImport;
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
        public async Task<IActionResult> UploadZip(IFormFile file)
        {
            if (!file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Only ZIP files are supported.");

            var currentUser = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "System";

            var command = new StartBulkImportCommand(file, currentUser);
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
