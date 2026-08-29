using ISDOX.DMS.Application.Common.Behaviors;
using ISDOX.DMS.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Auth.Commands
{
    public record ResetPasswordCommand(string Token, string NewPassword) : IRequest<bool>;

    public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, bool>
    {
        private readonly IDmsDbContext _context;
        private readonly IPasswordHasher _hasher;

        public ResetPasswordCommandHandler(IDmsDbContext context, IPasswordHasher hasher)
        {
            _context = context;
            _hasher = hasher;
        }

        public async Task<bool> Handle(ResetPasswordCommand request, CancellationToken ct)
        {
            var (isValid, message) = PasswordPolicy.Validate(request.NewPassword);
            if (!isValid)
            {
                throw new Exception(message); 
            }
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.ResetToken == request.Token && u.ResetTokenExpires > DateTime.Now, ct);

            if (user == null) 
            { 
                return false; 
            }

            user.PasswordHash = _hasher.HashPassword(request.NewPassword);

            user.PasswordLastChanged = DateTime.Now;
            user.ResetToken = null;
            user.ResetTokenExpires = null;

            var result = await _context.SaveChangesAsync(ct);

            return result > 0;
        }
    }
}
