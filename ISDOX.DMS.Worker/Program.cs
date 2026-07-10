using Amazon.S3;
using ISDOX.DMS.Application.Interfaces;
using ISDOX.DMS.Infrastructure.Persistence;
using ISDOX.DMS.Infrastructure.Services;
using ISDOX.DMS.Worker;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

var builder = Host.CreateApplicationBuilder(args);

builder.AddRabbitMQClient("messaging");

builder.Services.AddHostedService<DocumentProcessorWorker>();

builder.AddElasticsearchClient("elasticsearch");

builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var minioConnectionString = builder.Configuration.GetConnectionString("storage");

    if (string.IsNullOrEmpty(minioConnectionString))
    {
        throw new Exception("CRITICAL: The MinIO connection string is missing. Are you sure you are running the 'ISDOX.DMS.AppHost' project?");
    }

    var connectionBuilder = new DbConnectionStringBuilder
    {
        ConnectionString = minioConnectionString
    };

    var endpoint = connectionBuilder.ContainsKey("Endpoint") ? connectionBuilder["Endpoint"].ToString() : null;
    var accessKey = connectionBuilder.ContainsKey("AccessKey") ? connectionBuilder["AccessKey"].ToString() : "minioadmin";
    var secretKey = connectionBuilder.ContainsKey("SecretKey") ? connectionBuilder["SecretKey"].ToString() : "minioadmin";

    var config = new AmazonS3Config
    {
        ServiceURL = endpoint,
        ForcePathStyle = true,
        AuthenticationRegion = "ap-south-1" 
    };

    return new AmazonS3Client(accessKey, secretKey, config);
});
builder.AddNpgsqlDbContext<DmsDbContext>("IsdoxDmsDev", configureDbContextOptions: options =>
{
    options.UseNpgsql(sql => sql.MigrationsAssembly("ISDOX.DMS.Infrastructure"));
});

// 2. Map the Interface to the Aspire-registered DbContext
builder.Services.AddScoped<IDmsDbContext>(provider => provider.GetRequiredService<DmsDbContext>());
builder.Services.AddSingleton<IDocumentTextExtractor, DocumentTextExtractor>();
builder.Services.AddHostedService<BulkImportBackgroundService>();

var host = builder.Build();
host.Run();
