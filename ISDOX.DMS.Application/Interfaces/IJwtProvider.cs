using System.Security.Claims;

namespace ISDOX.DMS.Application.Interfaces
{
    public interface IJwtProvider
    {
        string GenerateToken(Guid userId, string username, string role, string Email);
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }
}
