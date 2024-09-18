using System.Net;
using System.Reflection;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OpenPrismNode;
using OpenPrismNode.Core;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.Crypto;
using OpenPrismNode.Core.Models.DidDocument;
using OpenPrismNode.Core.Services;
using OpenPrismNode.Sync.Services;
using OpenPrismNode.Web;
using NodeService = OpenPrismNode.Web.Services.NodeService;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new ServiceEndpointConverter());
    });


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "OpenPrismNode API", Version = "v1" });
   
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
    
    // Add the Authorization header
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Enter the authorization key",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
});

builder.WebHost.ConfigureKestrel(options =>
{
   // Listen for REST endpoints over HTTPS
    options.Listen(IPAddress.Any, 5001, listenOptions =>
    {
        listenOptions.UseHttps(); // Enforce HTTPS
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });

    // Listen for gRPC services over HTTP/2 without TLS
    options.Listen(IPAddress.Any, 50053, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = true;
});

var appSettingsSection = builder.Configuration.GetSection("AppSettings");
builder.Services.Configure<AppSettings>(appSettingsSection);
var appSettings = appSettingsSection.Get<AppSettings>();
builder.Services.AddSingleton<IEcService, EcServiceBouncyCastle>();
builder.Services.AddSingleton<ISha256Service, Sha256ServiceBouncyCastle>();
builder.Services.AddScoped<INpgsqlConnectionFactory, NpgsqlConnectionFactory>();
builder.Services.AddScoped<BackgroundSyncService>();
builder.Services.AddScoped<ICardanoWalletService, CardanoWalletService>();
builder.Services.AddSingleton<IWalletAddressCache>(new WalletAddressCache(appSettings!.WalletCacheSize));
builder.Services.AddSingleton<IStakeAddressCache>(new StakeAddressCache(appSettings.WalletCacheSize));
builder.Services.AddLazyCache();
builder.Services.AddHttpClient("CardanoWalletApi", client =>
{
    client.BaseAddress = new Uri("http://10.10.20.104:8090/v2/");
});
builder.Services.AddSingleton<BackgroundSyncService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<BackgroundSyncService>());
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies()));
builder.Services.AddDbContext<DataContext>(options =>
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
        .EnableSensitiveDataLogging(true)
        .UseNpgsql(appSettings!.PrismLedger.PrismPostgresConnectionString)
        .UseNpgsql(p =>
        {
            p.MigrationsAssembly("OpenPrismNode.Web");
            p.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(15),
                errorCodesToAdd: null
            );
        }));
builder.Services.AddLogging(p =>
    p.AddConsole()
        .AddSeq(builder.Configuration.GetSection("Seq"))
);

var app = builder.Build();

// Enable authorization if needed
app.UseAuthorization();

// Map gRPC service and controllers
app.MapGrpcService<OpenPrismNode.Web.Services.NodeService>();
app.MapControllers();

// Enable Swagger in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();