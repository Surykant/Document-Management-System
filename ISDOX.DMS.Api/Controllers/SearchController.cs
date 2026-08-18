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
        public async Task<IActionResult> Search(
            [FromQuery] string? q,
            [FromQuery] string? owner,
            [FromQuery] Guid? folderId,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] string? documentType) // <-- The new param
        {
            // Use named arguments to guarantee the exact mapping order
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
    }
}
