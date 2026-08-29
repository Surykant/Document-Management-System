using ISDOX.DMS.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Auth.Commands
{
    public record ForgotPasswordCommand(string Email) : IRequest<bool>;

    public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, bool>
    {
        private readonly IDmsDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IAuditLogger _auditLogger;

        public ForgotPasswordCommandHandler(IDmsDbContext context, IEmailService emailService, IAuditLogger auditLogger)
        {
            _context = context;
            _emailService = emailService;
            _auditLogger = auditLogger;
        }

        public async Task<bool> Handle(ForgotPasswordCommand request, CancellationToken ct)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct);
            if (user == null) 
            {
                await _auditLogger.LogAsync(
                    actionType: "Forget Password Request",
                    status: "Failed (User Not Found)",
                    overrideUserEmail: request.Email,
                    overrideUserId: user?.Id.ToString(),
                    ct: ct
                );
                return true;
            
            }

            user.ResetToken = Guid.NewGuid().ToString();
            user.ResetTokenExpires = DateTime.Now.AddHours(1);

            await _context.SaveChangesAsync(ct);
            await _emailService.SendResetEmailAsync(user.Email, user.ResetToken);

            await _auditLogger.LogAsync(
                    actionType: "Forget Password Request",
                    status: "Success",
                    overrideUserEmail: user.Email,
                    overrideUserId: user?.Id.ToString(),
                    ct: ct
                );

            return true;
        }
    }
}
