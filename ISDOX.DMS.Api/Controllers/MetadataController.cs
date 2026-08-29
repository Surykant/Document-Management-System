using ISDOX.DMS.Application.Metadata;
using ISDOX.DMS.Infrastructure.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISDOX.DMS.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class MetadataController : ControllerBase
    {
        private readonly IMediator _mediator;
        public MetadataController(IMediator mediator) => _mediator = mediator;

        [HttpPost("templates")]
        //[HasPermission("Admin.Metadata.Create")]
        public async Task<IActionResult> Create(CreateTemplateCommand command) => Ok(await _mediator.Send(command));

        [HttpGet("templates")]
       // [HasPermission("Document.View")]
        public async Task<IActionResult> GetAll() => Ok(await _mediator.Send(new GetAllTemplatesQuery()));

        [HttpPut("templates/{id}")]
        //[HasPermission("Admin.Metadata.Edit")]
        public async Task<IActionResult> Update(Guid id, UpdateTemplateCommand command)
        {
            if (id != command.Id) return BadRequest();
            return await _mediator.Send(command) ? Ok() : NotFound();
        }

        [HttpDelete("templates/{id}")]
       // [HasPermission("Admin.Metadata.Delete")]
        public async Task<IActionResult> Delete(Guid id) =>
            await _mediator.Send(new DeleteTemplateCommand(id)) ? NoContent() : NotFound();
    }
}