namespace ISDOX.DMS.Application.Interfaces
{
    public interface ICurrentUserService
    {
        string? UserId { get; }
        string? UserEmail { get; }
        string? IpAddress { get; }
        string? Device { get; }
        string? Browser { get; }
    }
}