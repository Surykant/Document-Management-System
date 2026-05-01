using ISDOX.DMS.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace ISDOX.DMS.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config) => _config = config;

        public async Task SendResetEmailAsync(string toEmail, string resetToken)
        {
            var smtpServer = _config["Email:Host"];
            var port = int.Parse(_config["Email:Port"] ?? "587");
            var fromEmail = _config["Email:From"];
            var password = _config["Email:Password"];

            // This is the link to your Angular/React frontend reset page
            var resetLink = $"{_config["Email:FrontendUrl"]}/reset-password?token={resetToken}";

            var message = new MailMessage(fromEmail!, toEmail)
            {
                Subject = "ISDOX DMS - Password Reset Request",
                Body = $"<h3>Password Reset</h3><p>Click the link below to reset your password. It expires in 1 hour.</p><a href='{resetLink}'>Reset Password</a>",
                IsBodyHtml = true
            };

            using var client = new SmtpClient(smtpServer, port)
            {
                Credentials = new NetworkCredential(fromEmail, password),
                EnableSsl = true
            };

            await client.SendMailAsync(message);
        }
    }
}
