using ISDOX.DMS.Application.Common.Behaviors;
using ISDOX.DMS.Application.Interfaces;
using ISDOX.DMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Users.Commands
{
    public record CreateUserCommand(
     string Username,
     string Name,
     string Email,
     string Password,
     string Department) : IRequest<Guid>;

    public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Guid>
    {
        private readonly IDmsDbContext _context;
        private readonly IAuditLogger _auditLogger;


        public CreateUserCommandHandler(IDmsDbContext context, IAuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        public async Task<Guid> Handle(CreateUserCommand request, CancellationToken ct)
        {
            var (isValid, message) = PasswordPolicy.Validate(request.Password);
            if (!isValid) throw new Exception(message);

            var exists = await _context.Users.AnyAsync(u => u.Email == request.Email || u.Username == request.Username, ct);
            if (exists)
                throw new Exception("User with this email or username already exists.");

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Username = request.Username,
                Name = request.Name,
                Email = request.Email,
                PasswordHash = passwordHash,
                Department = request.Department,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(ct);

            await _auditLogger.LogAsync(
                        actionType: "User Created",
                        entityId: user.Id,
                        entityName: user.Name,
                        // folderPath: user.,
                        status: "Success",
                        ct: ct
                    );

            return user.Id;
        }
    }
}
