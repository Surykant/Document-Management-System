using Amazon.S3;
using ISDOX.DMS.Application;
using ISDOX.DMS.Application.Interfaces;
using ISDOX.DMS.Infrastructure;
using ISDOX.DMS.Infrastructure.Authentication;
using ISDOX.DMS.Infrastructure.Messaging;
using ISDOX.DMS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Data.Common;
using System.Text;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8181, listenOptions =>
    {
        listenOptions.UseHttps(); 
    });
});

builder.AddServiceDefaults();

// 1. Add services from our Clean Architecture layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// 2. Add standard API services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors(options =>
{
    options.AddPolicy("OpenCors", policy =>
    {
        policy.AllowAnyOrigin()  
              .AllowAnyHeader() 
              .AllowAnyMethod(); 
    });
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!))
    };
});

builder.Services.AddAuthorization();

// Configure Swagger to accept JWT tokens 
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement

    {

        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()

    });
});

builder.AddNpgsqlDbContext<DmsDbContext>("IsdoxDmsDev", configureDbContextOptions: options =>
{
    options.UseNpgsql(sql => sql.MigrationsAssembly("ISDOX.DMS.Infrastructure"));
});

builder.AddRabbitMQClient("messaging");
builder.AddElasticsearchClient("elasticsearch");

// Register our custom publisher
builder.Services.AddScoped<IMessagePublisher, RabbitMqPublisher>();

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

builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<DmsDbContext>();

try
{
    var connString = builder.Configuration.GetConnectionString("IsdoxDmsDev");

    if (string.IsNullOrEmpty(connString))
    {
        app.Logger.LogWarning("Skipping EF Migrations: 'IsdoxDmsDev' connection string is missing.");
    }
    else
    {
        app.Logger.LogInformation("Applying EF Core Migrations...");
        db.Database.Migrate();
    }
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "An error occurred while migrating the database.");
}

app.UseHttpsRedirection();
app.UseCors("OpenCors");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();