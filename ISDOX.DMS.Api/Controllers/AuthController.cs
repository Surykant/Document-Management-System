using ISDOX.DMS.Application.Auth.Commands;
using ISDOX.DMS.Application.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace ISDOX.DMS.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginQuery query)
        {
            try
            {
                var token = await _mediator.Send(query);
                return Ok(new { Token = token, Message = "Login successful." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "An error occurred during login.", Error = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                return Unauthorized("Invalid user session.");
            }

            var result = await _mediator.Send(new LogoutCommand(userId));

            if (!result)
            {
                return BadRequest("Logout failed.");
            }

            return Ok(new { Message = "Logout successful. Session revoked." });
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            // Extract UserId from the JWT Claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            try
            {
                var command = new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword);
                var result = await _mediator.Send(command);

                return result ? Ok(new { Message = "Password changed successfully." })
                              : BadRequest("Could not change password.");
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var command = new RefreshTokenCommand(request.AccessToken, request.RefreshToken);
                var response = await _mediator.Send(command);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] string email)
        {
            await _mediator.Send(new ForgotPasswordCommand(email));
            return Ok(new { Message = "If your email is registered, you will receive a reset link." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var success = await _mediator.Send(new ResetPasswordCommand(request.Token, request.NewPassword));
            return success ? Ok("Password updated.") : BadRequest("Invalid or expired token.");
        }
    }
    public record RefreshTokenRequest(string AccessToken, string RefreshToken);
    public record ResetPasswordRequest(string Token, string NewPassword);
    public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
}
