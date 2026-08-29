using ISDOX.DMS.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Auth.Commands
{
    public record LogoutCommand(Guid UserId) : IRequest<bool>;

    public class LogoutCommandHandler : IRequestHandler<LogoutCommand, bool>
    {
        private readonly IDmsDbContext _context;
        private readonly IAuditLogger _auditLogger;

        public LogoutCommandHandler(IDmsDbContext context, IAuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        public async Task<bool> Handle(LogoutCommand request, CancellationToken ct)
        {
            var userTokens = await _context.RefreshTokens
                .Where(t => t.UserId == request.UserId)
                .ToListAsync(ct);

            if (!userTokens.Any())
            {
                return true;
            }

            _context.RefreshTokens.RemoveRange(userTokens);

            await _context.SaveChangesAsync(ct);

            await _auditLogger.LogAsync(
                        actionType: "Logout",
                        status: "Success",
                        ct: ct
                    );
            return true;
        }
    }
}
