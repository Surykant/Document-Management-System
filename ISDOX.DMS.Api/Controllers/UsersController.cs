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
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] string? searchTerm,
            [FromQuery] string? roleName,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = new GetAllUsersQuery(searchTerm, roleName, pageNumber, pageSize);

            var result = await _mediator.Send(query);

            return Ok(result);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            var query = new GetUserByIdQuery(id);
            var result = await _mediator.Send(query);

            if (result == null) return NotFound("User not found.");

            return Ok(result);
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
                    request.Name,
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
    public record UpdateUserRequest(string Username, string Name, string Email, string Department, bool IsActive);
}
