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
        private readonly IAuditLogger _auditLogger;

        public ChangePasswordCommandHandler(IDmsDbContext context, IPasswordHasher hasher, IAuditLogger auditLogger)
        {
            _context = context;
            _hasher = hasher;
            _auditLogger = auditLogger;
        }

        public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken ct)
        {
            var (isValid, message) = PasswordPolicy.Validate(request.NewPassword);
            if (!isValid) throw new Exception(message);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct);
            if (user == null) 
            {
                await _auditLogger.LogAsync(
                    actionType: "Change Password",
                    status: "Failed",
                    overrideUserEmail: user.Email,
                    overrideUserId: user?.Id.ToString(),
                    ct: ct
                );
                throw new Exception("User not found.");             
            }

            bool isCurrentPasswordValid = _hasher.VerifyPassword(request.CurrentPassword, user.PasswordHash);
            if (!isCurrentPasswordValid)
            {
                await _auditLogger.LogAsync(
                    actionType: "Change Password",
                    status: "Failed",
                    overrideUserEmail: user.Email,
                    overrideUserId: user?.Id.ToString(),
                    ct: ct
                );
                throw new Exception("The current password you entered is incorrect.");
            }

            user.PasswordHash = _hasher.HashPassword(request.NewPassword);
            user.PasswordLastChanged = DateTime.Now; 

            await _context.SaveChangesAsync(ct);

            await _auditLogger.LogAsync(
            actionType: "Change Password",
            status: "Success",
            overrideUserEmail: user.Email, 
            overrideUserId: user?.Id.ToString(), 
            ct: ct
        );
            return true;
        }
    }
}
