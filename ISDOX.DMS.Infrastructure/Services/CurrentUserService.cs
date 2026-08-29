using System.Security.Claims;
using ISDOX.DMS.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using UAParser;

namespace ISDOX.DMS.Infrastructure.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        public string? UserEmail => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Email)
                                 ?? _httpContextAccessor.HttpContext?.User?.Identity?.Name;

        public string? IpAddress
        {
            get
            {
                var ip = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress;

                if (ip == null) return null;

                if (ip.IsIPv4MappedToIPv6 || ip.ToString() == "::1")
                {
                    return ip.MapToIPv4().ToString();
                }

                return ip.ToString();
            }
        }

        public string? Browser
        {
            get
            {
                var userAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();
                if (string.IsNullOrEmpty(userAgent)) return "Unknown";
                if (userAgent.Contains("Chrome")) return "Google Chrome";
                if (userAgent.Contains("Firefox")) return "Mozilla Firefox";
                if (userAgent.Contains("Safari") && !userAgent.Contains("Chrome")) return "Apple Safari";
                if (userAgent.Contains("Edg")) return "Microsoft Edge";
                return "Unknown Browser";
            }
        }

        public string? Device
        {
            get
            {
                var userAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();
                if (string.IsNullOrEmpty(userAgent)) return "Unknown";
                if (userAgent.Contains("Windows")) return "Windows Desktop";
                if (userAgent.Contains("Macintosh")) return "Mac Desktop";
                if (userAgent.Contains("iPhone") || userAgent.Contains("iPad")) return "iOS Device";
                if (userAgent.Contains("Android")) return "Android Device";
                return "Unknown Device";
            }
        }
    }
}
