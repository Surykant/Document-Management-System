using ISDOX.DMS.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Users.Commands
{
    public record UpdateUserCommand(
         Guid Id,
         string Username,
         string Name,
         string Email,
         string Department,
         bool IsActive) : IRequest<bool>;

    public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, bool>
    {
        private readonly IDmsDbContext _context;
        private readonly IAuditLogger _auditLogger;


        public UpdateUserCommandHandler(IDmsDbContext context, IAuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        public async Task<bool> Handle(UpdateUserCommand request, CancellationToken ct)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.Id, ct);

            if (user == null) return false;

            if (user.Email != request.Email &&
                await _context.Users.AnyAsync(u => u.Email == request.Email, ct))
            {
                throw new Exception("Email is already in use by another user.");
            }

            user.Username = request.Username;
            user.Name = request.Name;
            user.Email = request.Email;
            user.Department = request.Department;
            user.IsActive = request.IsActive;

            var result = await _context.SaveChangesAsync(ct);

            await _auditLogger.LogAsync(
                       actionType: "User Updated",
                       entityId: user.Id,
                       entityName: user.Name,
                       // folderPath: user.,
                       status: "Success",
                       ct: ct
                   );
            return result > 0;
        }
    }
}
