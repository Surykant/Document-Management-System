using ISDOX.DMS.Application.Folders.Commands;
using ISDOX.DMS.Application.Folders.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ISDOX.DMS.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FoldersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public FoldersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("tree")]
        public async Task<ActionResult<List<FolderNodeDto>>> GetTree()
        {
            var query = new GetFolderTreeQuery();
            var result = await _mediator.Send(query);
            return Ok(result);
        }
        [HttpPost]
        public async Task<IActionResult> CreateFolder([FromBody] CreateFolderRequest request)
        {
            var createdBy = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Unknown User";

            var command = new CreateFolderCommand(request.Name, request.ParentId, createdBy);
            var folderId = await _mediator.Send(command);

            return Ok(new { FolderId = folderId });
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> Rename(Guid id, [FromBody] string newName)
        {
            var result = await _mediator.Send(new UpdateFolderCommand(id, newName));
            return result ? Ok() : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var result = await _mediator.Send(new DeleteFolderCommand(id));
                return result ? NoContent() : NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
    public class CreateFolderRequest
    {
        public string Name { get; set; } = string.Empty;
        public Guid? ParentId { get; set; }
    }
}
