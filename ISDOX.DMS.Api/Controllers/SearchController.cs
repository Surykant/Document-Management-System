using ISDOX.DMS.Application.Interfaces;
using ISDOX.DMS.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISDOX.DMS.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _searchService;

        public SearchController(ISearchService searchService) => _searchService = searchService;

        [HttpGet("documents")]
        [HasPermission("Document.View")]
        public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] string? owner, [FromQuery] Guid? folderId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            var results = await _searchService.SearchDocumentsAsync(q ?? "*", owner, folderId, fromDate, toDate);

            return Ok(results);
        }
    }
}
