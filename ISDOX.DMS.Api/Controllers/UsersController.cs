using ISDOX.DMS.Application.Interfaces;
using ISDOX.DMS.Application.Users.Commands;
using ISDOX.DMS.Application.Users.Queries;
using ISDOX.DMS.Domain.Models.Search;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        [HttpPost("reindex-all")]
        public async Task<IActionResult> ReindexAllDocuments(
    [FromServices] IDmsDbContext context,
    [FromServices] ISearchService searchService,
    [FromServices] IDocumentTextExtractor textExtractor, // Assuming you have this injected
    CancellationToken ct)
        {
            // 1. Get all documents from the database
            var allDocs = await context.Documents
                .Include(d => d.Versions)
                .ToListAsync(ct);

            int count = 0;

            foreach (var doc in allDocs)
            {
                var latestVersion = doc.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
                if (latestVersion == null) continue;

                // NOTE: In a real scenario, you'd need to re-download the file from MinIO 
                // to extract the text again. For a quick sync, we'll just index the metadata.

                var searchModel = new DocumentSearchModel
                {
                    Id = doc.Id,
                    Name = doc.Name,
                    Description = doc.Description,
                    FolderId = doc.FolderId,
                    Owner = doc.Owner,
                    CreatedAt = doc.CreatedAt,
                    FileExtension = latestVersion.FileExtension,
                    VersionNumber = latestVersion.VersionNumber,
                    //Content = "" // Left empty unless you re-extract the text from MinIO
                };

                // 2. Push to Elasticsearch
                await searchService.IndexDocumentAsync(searchModel);
                count++;
            }

            return Ok(new { Message = $"Successfully pushed {count} documents to Elasticsearch." });
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
