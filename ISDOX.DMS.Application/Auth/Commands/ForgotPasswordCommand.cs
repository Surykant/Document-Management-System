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

        public ForgotPasswordCommandHandler(IDmsDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<bool> Handle(ForgotPasswordCommand request, CancellationToken ct)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct);
            if (user == null) return true;

            user.ResetToken = Guid.NewGuid().ToString();
            user.ResetTokenExpires = DateTime.Now.AddHours(1);

            await _context.SaveChangesAsync(ct);
            await _emailService.SendResetEmailAsync(user.Email, user.ResetToken);

            return true;
        }
    }
}
