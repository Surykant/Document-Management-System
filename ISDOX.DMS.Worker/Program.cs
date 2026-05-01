using Amazon.S3;
using ISDOX.DMS.Worker;
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

var host = builder.Build();
host.Run();
