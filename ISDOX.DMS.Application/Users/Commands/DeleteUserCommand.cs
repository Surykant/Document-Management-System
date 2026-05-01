using ISDOX.DMS.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ISDOX.DMS.Application.Users.Commands
{
    public record DeleteUserCommand(Guid Id) : IRequest<bool>;

    public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, bool>
    {
        private readonly IDmsDbContext _context;

        public DeleteUserCommandHandler(IDmsDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(DeleteUserCommand request, CancellationToken ct)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.Id, ct);

            if (user == null) return false;

             user.IsActive = false; 
             user.DeletedAt = DateTime.Now;
            

            var result = await _context.SaveChangesAsync(ct);
            return result > 0;
        }
    }
}
