using ISDOX.DMS.Application.Auth.Common;
using ISDOX.DMS.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;
using System.Security.Cryptography;

namespace ISDOX.DMS.Application.Auth.Commands
{
    public record RefreshTokenCommand(string AccessToken, string RefreshToken) : IRequest<AuthenticationResponse>;

    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthenticationResponse>
    {
        private readonly IDmsDbContext _context;
        private readonly IJwtProvider _jwtProvider; 

        public RefreshTokenCommandHandler(IDmsDbContext context, IJwtProvider jwtProvider)
        {
            _context = context;
            _jwtProvider = jwtProvider;
        }

        public async Task<AuthenticationResponse> Handle(RefreshTokenCommand request, CancellationToken ct)
        {
            var principal = _jwtProvider.GetPrincipalFromExpiredToken(request.AccessToken);
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value; 

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                throw new Exception("Invalid token claims.");

            var savedRefreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == request.RefreshToken && t.UserId == userId, ct);

            if (savedRefreshToken == null || savedRefreshToken.ExpiryTime <= DateTime.UtcNow)
                throw new Exception("Invalid or expired refresh token.");

            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstAsync(u => u.Id == userId, ct);

            var userRole = user.UserRoles.FirstOrDefault()?.Role?.Name ?? "User";

            var newAccessToken = _jwtProvider.GenerateToken(user.Id, user.Username, userRole, user.Email);
            var newRefreshTokenString = GenerateRefreshTokenString();

            _context.RefreshTokens.Remove(savedRefreshToken);
            _context.RefreshTokens.Add(new ISDOX.DMS.Domain.Entities.RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = newRefreshTokenString,
                UserId = userId,
                ExpiryTime = DateTime.Now.AddDays(7)
            });

            await _context.SaveChangesAsync(ct);

            return new AuthenticationResponse(newAccessToken, newRefreshTokenString);
        }

        private string GenerateRefreshTokenString()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
