using ISDOX.DMS.Application.Interfaces;
using ISDOX.DMS.Infrastructure.Authentication;
using ISDOX.DMS.Infrastructure.Logging;
using ISDOX.DMS.Infrastructure.Messaging;
using ISDOX.DMS.Infrastructure.Persistence;
using ISDOX.DMS.Infrastructure.Services;
using ISDOX.DMS.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ISDOX.DMS.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IDmsDbContext>(provider => provider.GetRequiredService<DmsDbContext>());

            services.AddTransient<IStorageService, MinioStorageService>();
            services.AddScoped<IJwtProvider, JwtProvider>();
            services.AddScoped<IMessagePublisher, RabbitMqPublisher>();
            services.AddScoped<IPasswordHasher, PasswordHasher>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddSingleton<IDocumentTextExtractor, DocumentTextExtractor>();
            services.AddHttpContextAccessor();
            services.AddScoped<IAuditLogger, AuditLogger>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            return services;
        }
    }
}