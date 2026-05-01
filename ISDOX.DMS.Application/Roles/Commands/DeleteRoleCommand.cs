using ISDOX.DMS.Application.Interfaces;
using MediatR;

namespace ISDOX.DMS.Application.Roles.Commands
{
    public record DeleteRoleCommand(Guid Id) : IRequest<bool>;

    public class DeleteRoleCommandHandler : IRequestHandler<DeleteRoleCommand, bool>
    {
        private readonly IDmsDbContext _context;
        public DeleteRoleCommandHandler(IDmsDbContext context) => _context = context;

        public async Task<bool> Handle(DeleteRoleCommand request, CancellationToken ct)
        {
            var role = await _context.Roles.FindAsync(new object[] { request.Id }, ct);
            if (role == null) return false;

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync(ct);
            return true;
        }
    }
}
