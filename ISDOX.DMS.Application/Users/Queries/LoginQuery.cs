using ISDOX.DMS.Application.Interfaces;
using ISDOX.DMS.Domain.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace ISDOX.DMS.Application.Users.Queries
{
    public record LoginQuery(string UsernameOrEmail, string Password) : IRequest<LoginResponseDto>;

    public class LoginQueryHandler : IRequestHandler<LoginQuery, LoginResponseDto>
    {
        private readonly IDmsDbContext _context;
        private readonly IJwtProvider _jwtProvider;

        public LoginQueryHandler(IDmsDbContext context, IJwtProvider jwtProvider)
        {
            _context = context;
            _jwtProvider = jwtProvider;
        }

        public async Task<LoginResponseDto> Handle(LoginQuery request, CancellationToken ct)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username == request.UsernameOrEmail || u.Email == request.UsernameOrEmail, ct);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Invalid credentials.");

            if (!user.IsActive)
                throw new UnauthorizedAccessException("User account is disabled.");

            var expiryDays = 30;
            if (user.PasswordLastChanged.AddDays(expiryDays) < DateTime.Now)
            {
                throw new Exception("Password has expired. Please reset your password.");
            }

            var userRole = user.UserRoles.FirstOrDefault()?.Role.Name ?? "Standard User";

            var token = _jwtProvider.GenerateToken(user.Id, user.Username, userRole, user.Email);
            var refreshToken = GenerateRefreshToken();

            var expiredTokens = _context.RefreshTokens.Where(t => t.ExpiryTime < DateTime.Now).ToList();
            _context.RefreshTokens.RemoveRange(expiredTokens);

            _context.RefreshTokens.Add(new ISDOX.DMS.Domain.Entities.RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = refreshToken,
                UserId = user.Id,
                ExpiryTime = DateTime.Now.AddDays(7)
            });

            await _context.SaveChangesAsync(ct);

            return new LoginResponseDto
            {
                AccessToken = token,
                RefreshToken = refreshToken
            };
        }
        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
