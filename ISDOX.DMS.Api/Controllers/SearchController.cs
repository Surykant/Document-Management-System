using ISDOX.DMS.Application.Documents.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ISDOX.DMS.Api.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly IMediator _mediator;

        public SearchController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("documents")]
        // [HasPermission("Document.View")]
        public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] string? owner, [FromQuery] Guid? folderId, [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] string? documentType) 
        {
            var query = new SearchDocumentsQuery(
                Keyword: q ?? "*",
                Owner: owner,
                FolderId: folderId,
                FromDate: fromDate,
                ToDate: toDate,
                DocumentType: documentType
            );

            var results = await _mediator.Send(query);
            return Ok(results);
        }
        [HttpPost("advanced-search")]
        public async Task<IActionResult> AdvancedSearch([FromBody] AdvancedSearchDocumentsQuery request)
        {
            if (request == null || request.TemplateId == Guid.Empty)
            {
                return BadRequest(new { Error = "Invalid payload. TemplateId is required." });
            }

            var results = await _mediator.Send(request);

            return Ok(results);
        }
    }
}
