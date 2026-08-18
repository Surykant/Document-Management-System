using ISDOX.DMS.Application.Interfaces;
using ISDOX.DMS.Application.Permissions.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Api.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class PermissionsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IDmsDbContext _context; 

        public PermissionsController(IMediator mediator, IDmsDbContext context)
        {
            _mediator = mediator;
            _context = context;
        }

        [HttpPost] 
        public async Task<IActionResult> Create([FromBody] CreatePermissionCommand command)
        {
            return Ok(await _mediator.Send(command));
        }

        [HttpGet] 
        public async Task<IActionResult> GetAll()
        {
            var permissions = await _context.Permissions.ToListAsync();
            return Ok(permissions);
        }
        public class AssignPermissionsDto
        {
            public List<Guid> PermissionIds { get; set; } = new();
        }

        [HttpPost("/api/roles/{roleId:guid}/permissions")]
        public async Task<IActionResult> AssignPermissions(Guid roleId, [FromBody] AssignPermissionsDto request)
        {
            if (request.PermissionIds == null || !request.PermissionIds.Any())
                return BadRequest(new { Error = "At least one Permission ID must be provided." });

            var command = new AssignPermissionsToRoleCommand(roleId, request.PermissionIds);
            await _mediator.Send(command);

            return Ok(new { Message = "Permissions assigned successfully." });
        }
    }
}
