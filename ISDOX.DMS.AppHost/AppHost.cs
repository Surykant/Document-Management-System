var builder = DistributedApplication.CreateBuilder(args);

// 1. Database
var database = builder.AddPostgres("postgres")
                      .WithDataVolume("pg-data")
                      .WithPgAdmin()
                      .AddDatabase("IsdoxDmsDev");

// 2. Message Queue
var messaging = builder.AddRabbitMQ("messaging")
                       .WithDataVolume("rmq-data");

// 3. Search Engine
var elasticsearch = builder.AddElasticsearch("elasticsearch")
                           .WithImageTag("8.19.4")
                           .WithDataVolume("es-data-v3")
                           .WithEnvironment("ES_JAVA_OPTS", "-Xms512m -Xmx512m")
                           .WithLifetime(ContainerLifetime.Persistent); 

// 4. Object Storage (MinIO)
var minio = builder.AddMinioContainer("storage")
                   .WithDataVolume("minio-data");

// 5. API Project
var api = builder.AddProject<Projects.ISDOX_DMS_Api>("api")
    .WithEndpoint(port: 8443, scheme: "https", name: "lan-https-endpoint", isProxied: false)
    .WithExternalHttpEndpoints()
    .WithReference(database)
    .WithReference(messaging)
    .WithReference(elasticsearch)
    .WithReference(minio);

// 6. Worker Project
var worker = builder.AddProject<Projects.ISDOX_DMS_Worker>("worker")
    .WithReference(database)
    .WithReference(messaging)
    .WithReference(elasticsearch)
    .WithReference(minio);

builder.Build().Run();