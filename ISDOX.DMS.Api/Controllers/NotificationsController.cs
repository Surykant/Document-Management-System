using ISDOX.DMS.Application.Notifications.Commands;
using ISDOX.DMS.Application.Notifications.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ISDOX.DMS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] 
    public class NotificationsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public NotificationsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        private string GetCurrentUserId()
            => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();

        [HttpPost]
        public async Task<IActionResult> PushNotification([FromBody] CreateNotificationCommand command)
        {
            await _mediator.Send(command);
            return Ok(new { Message = "Notification dispatched." });
        }

        [HttpGet]
        public async Task<IActionResult> GetMyNotifications(
            [FromQuery] string type = "All", [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var query = new GetUserNotificationsQuery(GetCurrentUserId(), type, pageNumber, pageSize);
            var results = await _mediator.Send(query);
            return Ok(results);
        }

        [HttpPut("{id:guid}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var command = new MarkNotificationsAsReadCommand(GetCurrentUserId(), id);
            await _mediator.Send(command);
            return NoContent();
        }

        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var command = new MarkNotificationsAsReadCommand(GetCurrentUserId());
            await _mediator.Send(command);
            return NoContent();
        }
    }
}