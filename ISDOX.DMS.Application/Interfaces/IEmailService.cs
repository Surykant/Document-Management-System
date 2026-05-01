namespace ISDOX.DMS.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendResetEmailAsync(string toEmail, string resetToken);
    }
}
