using ISDOX.DMS.Application.Interfaces;
using ISDOX.DMS.Domain.Entities;
using MediatR;
using System.Security.Cryptography;

namespace ISDOX.DMS.Application.Documents.Commands
{
    public record ShareDocumentCommand(
        Guid DocumentId,
        DateTime? ExpiryDate,
        bool IsPasswordProtected,
        string? Password,
        string CreatedBy
    ) : IRequest<string>;

    public class ShareDocumentCommandHandler : IRequestHandler<ShareDocumentCommand, string>
    {
        private readonly IDmsDbContext _context;
        private readonly IAuditLogger _auditLogger;


        public ShareDocumentCommandHandler(IDmsDbContext context, IAuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        public async Task<string> Handle(ShareDocumentCommand request, CancellationToken ct)
        {
            var document = await _context.Documents.FindAsync(new object[] { request.DocumentId }, ct);
            if (document == null) throw new Exception("Document not found.");

            var token = GenerateSecureToken();

            string? passwordHash = null;
            if (request.IsPasswordProtected && !string.IsNullOrWhiteSpace(request.Password))
            {
                passwordHash = request.Password; 
            }

            var documentShare = new DocumentShare
            {
                Id = Guid.NewGuid(),
                DocumentId = request.DocumentId,
                Token = token,
                IsPasswordProtected = request.IsPasswordProtected,
                PasswordHash = passwordHash,
                ExpiryDate = request.ExpiryDate?.ToLocalTime(), 
                IsRevoked = false,
                CreatedAt = DateTime.Now,
                CreatedBy = request.CreatedBy
            };

            _context.DocumentShares.Add(documentShare);
            await _context.SaveChangesAsync(ct);

            await _auditLogger.LogAsync(
                        actionType: "Document Shared",
                        entityId: document.Id,
                        entityName: document.Name,
                        // folderPath: document.,
                        status: "Success",
                        ct: ct
                    );
            return token;
        }

        private static string GenerateSecureToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(12);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }
    }
}