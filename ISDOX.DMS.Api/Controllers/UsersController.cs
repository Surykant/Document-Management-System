using ISDOX.DMS.Application.Users.Commands;
using ISDOX.DMS.Application.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ISDOX.DMS.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize] 
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UsersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _mediator.Send(new GetAllUsersQuery());
            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var user = await _mediator.Send(new GetUserByIdQuery(id));

            if (user == null)
                return NotFound(new { Message = "User not found." });

            return Ok(user);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserCommand command)
        {
            try
            {
                var userId = await _mediator.Send(command);
                return Ok(new { UserId = userId, Message = "User created successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
        {
            try
            {
                var command = new UpdateUserCommand(
                    id,
                    request.Username,
                    request.Email,
                    request.Department,
                    request.IsActive);

                var success = await _mediator.Send(command);

                if (!success) return NotFound($"User with ID {id} not found.");

                return Ok(new { Message = "User updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var success = await _mediator.Send(new DeleteUserCommand(id));

            if (!success)
            {
                return NotFound(new { Message = $"User with ID {id} not found." });
            }

            return Ok(new { Message = "User deleted successfully." });
        }
    }
    public record UpdateUserRequest(string Username, string Email, string Department, bool IsActive);
}
