using ISDOX.DMS.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Users.Commands
{
    public record DeleteUserCommand(Guid Id) : IRequest<bool>;

    public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, bool>
    {
        private readonly IDmsDbContext _context;
        private readonly IAuditLogger _auditLogger;


        public DeleteUserCommandHandler(IDmsDbContext context, IAuditLogger auditLogger)
        {
            _context = context;
            _auditLogger = auditLogger;
        }

        public async Task<bool> Handle(DeleteUserCommand request, CancellationToken ct)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.Id, ct);

            if (user == null) return false;

             user.IsActive = false; 
             user.DeletedAt = DateTime.Now;
            

            var result = await _context.SaveChangesAsync(ct);

            await _auditLogger.LogAsync(
                        actionType: "User Deleted",
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
