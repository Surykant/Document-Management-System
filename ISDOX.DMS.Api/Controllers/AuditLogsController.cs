using ISDOX.DMS.Application.AuditLogs.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISDOX.DMS.Api.Controllers
{
    [ApiController]
    [Route("api/audit-logs")]
    [Authorize] 
    public class AuditLogsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuditLogsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAuditLogs([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? userId = null, [FromQuery] string? actionType = null)
        {
            var query = new GetAuditLogsQuery(pageNumber, pageSize, userId, actionType);
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserAuditProfile(string userId)
        {
            var query = new GetUserAuditProfileQuery(userId);
            var result = await _mediator.Send(query);

            if (result == null) return NotFound(new { Error = "No audit history found for this user." });

            return Ok(result);
        }
    }
}