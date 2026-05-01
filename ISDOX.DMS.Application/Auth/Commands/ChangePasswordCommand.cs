using ISDOX.DMS.Application.Common.Behaviors;
using ISDOX.DMS.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace ISDOX.DMS.Application.Auth.Commands
{
    public record ChangePasswordCommand(
        Guid UserId,
        string CurrentPassword,
        string NewPassword) : IRequest<bool>;

    public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, bool>
    {
        private readonly IDmsDbContext _context;
        private readonly IPasswordHasher _hasher;

        public ChangePasswordCommandHandler(IDmsDbContext context, IPasswordHasher hasher)
        {
            _context = context;
            _hasher = hasher;
        }

        public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken ct)
        {
            var (isValid, message) = PasswordPolicy.Validate(request.NewPassword);
            if (!isValid) throw new Exception(message);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct);
            if (user == null) throw new Exception("User not found.");

            bool isCurrentPasswordValid = _hasher.VerifyPassword(request.CurrentPassword, user.PasswordHash);
            if (!isCurrentPasswordValid)
            {
                throw new Exception("The current password you entered is incorrect.");
            }

            user.PasswordHash = _hasher.HashPassword(request.NewPassword);
            user.PasswordLastChanged = DateTime.Now; 

            await _context.SaveChangesAsync(ct);
            return true;
        }
    }
}
