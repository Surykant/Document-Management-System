using ISDOX.DMS.Application.Common.Behaviors;
using ISDOX.DMS.Application.Documents.Commands;
using ISDOX.DMS.Application.Documents.Queries;
using ISDOX.DMS.Application.Interfaces;
using ISDOX.DMS.Infrastructure.Authentication;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ISDOX.DMS.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DocumentsController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IDmsDbContext _context;

        public DocumentsController(IMediator mediator, IDmsDbContext context)
        {
            _mediator = mediator;
            _context = context;
        }

        [HttpGet]
        [HasPermission("Document.View")]
        public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? searchTerm = null, [FromQuery] string? ownerName = null, [FromQuery] string sortBy = "CreatedAt", [FromQuery] bool isDescending = true)
        {
            var docs = await _mediator.Send(new GetAllDocumentsQuery());
            return Ok(docs);
        }

        [HttpGet("{id}")]
        [HasPermission("Document.View")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var doc = await _mediator.Send(new GetDocumentByIdQuery(id));
            return doc != null ? Ok(doc) : NotFound();
        }

        [HttpGet("folder/{folderId}")]
        [HasPermission("Document.View")]
        public async Task<IActionResult> GetByFolder(Guid folderId)
        {
            var docs = await _mediator.Send(new GetDocumentsByFolderQuery(folderId));
            return Ok(docs);
        }

        [HttpGet("{id}/versions")]
        public async Task<IActionResult> GetDocumentVersions(Guid id)
        {
            try
            {
                var versions = await _mediator.Send(new GetDocumentVersionsQuery(id));
                return Ok(versions);
            }
            catch (Exception ex)
            {
                return NotFound(new { Error = ex.Message });
            }
        }

        [HttpGet("{id}/versions/{versionNumber}/download")]
        [HasPermission("Document.Download")]
        public async Task<IActionResult> DownloadSpecificVersion(Guid id, int versionNumber)
        {
            try
            {
                var result = await _mediator.Send(new DownloadSpecificVersionQuery(id, versionNumber));

                return File(result.Content, result.ContentType, result.DownloadFileName);
            }
            catch (Exception ex)
            {
                return NotFound(new { Error = ex.Message });
            }
        }

        [HttpGet("{id}/download")]
        [HasPermission("Document.Download")]
        public async Task<IActionResult> DownloadDocument(Guid id)
        {
            try
            {
                var result = await _mediator.Send(new DownloadDocumentQuery(id));

                return File(result.Content, result.ContentType, result.DownloadFileName);
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(new { Message = "Requested file not found..", Error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "An error occurred while downloading the document.", Error = ex.Message });
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchDocuments([FromQuery] string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return BadRequest(new { Error = "Search keyword cannot be empty." });

            try
            {
                var query = new SearchDocumentsQuery(
             Keyword: keyword,
             Owner: null,
             FolderId: null,
             FromDate: null,
             ToDate: null,
             DocumentType: null
         );
                var results = await _mediator.Send(query);

                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An error occurred while searching documents.", Error = ex.Message });
            }
        }

        [HttpGet("{id}/versionshistory")]
        [HasPermission("Document.View")]
        public async Task<IActionResult> GetVersions(Guid id)
        {
            var history = await _mediator.Send(new GetVersionHistoryQuery(id));
            return Ok(history);
        }

        [HttpGet("{id}/metadata")]
        [HasPermission("Document.View")]
        public async Task<IActionResult> GetMetadata(Guid id)
        {
            var metadata = await _mediator.Send(new GetDocumentMetadataQuery(id));
            return metadata != null ? Ok(metadata) : NotFound();
        }

        [HttpPost("{id}/metadata")]
        [HttpPut("{id}/metadata")]
        [HasPermission("Document.Edit")]
        public async Task<IActionResult> UpsertMetadata(Guid id, [FromBody] Dictionary<string, string> metadata)
        {
            var success = await _mediator.Send(new UpdateMetadataOnlyCommand(id, metadata));
            return success ? Ok(new { Message = "Metadata updated." }) : NotFound();
        }

        [HttpPost("{id}/versions")]
        [HasPermission("Document.Edit")]
        public async Task<IActionResult> AddVersion(Guid id, [FromForm] string description, [FromForm] string createdBy, IFormFile file)
        {
            var doc = await _context.Documents.FindAsync(id);
            if (doc == null) return NotFound();

            using var stream = file.OpenReadStream();

            var command = new CreateDocumentCommand(
                Name: doc.Name,
                Description: description,
                FolderId: doc.FolderId ?? Guid.Empty,
                File: file,
                CreatedBy: createdBy,
                Metadata: null
            );

            await _mediator.Send(command);
            return Ok(new { Message = "New version uploaded successfully." });
        }

        [HttpPost("{id}/restore-version/{versionId}")]
        [HasPermission("Document.Edit")]
        public async Task<IActionResult> Restore(Guid id, Guid versionId, [FromQuery] string requestedBy)
        {
            var success = await _mediator.Send(new RestoreVersionCommand(id, versionId, requestedBy));
            return success ? Ok(new { Message = "Version restored." }) : NotFound();
        }

        [HttpPost("upload")]
        [HasPermission("Document.Edit")]
        public async Task<IActionResult> UploadDocument([FromForm] string name, [FromForm] string description, [FromForm] Guid folderId, [FromForm] string createdBy, [FromForm] string? metadataJson, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty or missing.");

            if (!SupportedFileTypes.IsSupported(file.FileName))
                return BadRequest($"File type not supported. Allowed: {string.Join(", ", SupportedFileTypes.AllowedExtensions)}");

            Dictionary<string, string>? metadata = null;

            if (!string.IsNullOrWhiteSpace(metadataJson))
            {
                try
                {
                    var cleanJson = metadataJson.Trim();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    metadata = JsonSerializer.Deserialize<Dictionary<string, string>>(cleanJson, options);
                } 
                catch (JsonException ex)
                {
                    Console.WriteLine($"JSON Parsing Error: {ex.Message}. String received: {metadataJson}");
                    return BadRequest(new { Message = "Invalid metadata format.", ReceivedValue = metadataJson }); 
                }
            }

            var command = new CreateDocumentCommand(
                Name: name,
                Description: description,
                FolderId: folderId,
                File: file,
                CreatedBy: createdBy,
                Metadata: metadata ?? new Dictionary<string, string>() 
            );

            var documentId = await _mediator.Send(command);

            return Ok(new { DocumentId = documentId, Message = "Document uploaded successfully." });
        }

        [HttpPatch("{id}/move")]
        public async Task<IActionResult> MoveDocument(Guid id, [FromBody] Guid? targetFolderId)
        {
            var command = new MoveDocumentCommand(id, targetFolderId);
            var success = await _mediator.Send(command);

            if (!success) return NotFound("Document not found.");

            return Ok(new { Message = "Document moved successfully." }); 
        }

        [HttpPut("{id}")]
        [HasPermission("Document.Edit")]
        public async Task<IActionResult> UpdateMetadata(Guid id, [FromBody] UpdateDocumentRequest request)
        {
            var success = await _mediator.Send(new UpdateDocumentMetadataCommand(
                id,
                request.Name,
                request.Description,
                request.Metadata));

            if (!success) return NotFound(new { Message = "Document not found." });

            return Ok(new { Message = "Document updated successfully." });
        }

        [HttpDelete("{id}")]
        [HasPermission("Document.Delete")]
        public async Task<IActionResult> DeleteDocument(Guid id)
        {
            var success = await _mediator.Send(new DeleteDocumentCommand(id));

            if (!success)
                return NotFound(new { Error = "Document not found." });

            return Ok(new { Message = "Document deleted." });
        }

    }
    public record UpdateDocumentRequest(
    string Name,
    string Description,
    Dictionary<string, string>? Metadata);
}