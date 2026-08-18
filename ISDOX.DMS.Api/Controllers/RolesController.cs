using ISDOX.DMS.Application.Roles.Commands;
using ISDOX.DMS.Application.Roles.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ISDOX.DMS.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(Roles = "Admin")]
    public class RolesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public RolesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRoleCommand command)
        {
            var result = await _mediator.Send(command);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var roles = await _mediator.Send(new GetAllRolesQuery());
            return Ok(roles);
        }
        [HttpGet("{roleId:guid}/permissions")]
        public async Task<IActionResult> GetRolePermissions(Guid roleId)
        {
            if (roleId == Guid.Empty)
                return BadRequest(new { Error = "Invalid Role ID." });

            var query = new GetRolePermissionsQuery(roleId);
            var results = await _mediator.Send(query);

            // Returns a 200 OK with the array of permission objects (Id, Name, Description)
            return Ok(results);
        }
        [HttpPost("assign")]
        public async Task<IActionResult> Assign([FromBody] AssignRoleCommand command)
        {
            await _mediator.Send(command);
            return Ok(new { Message = "Role assigned successfully." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var success = await _mediator.Send(new DeleteRoleCommand(id));
            if (!success) return NotFound();

            return Ok(new { Message = "Role deleted successfully." });
        }
    }
}
