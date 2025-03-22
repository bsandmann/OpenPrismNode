using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json.Serialization;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.OpenApi.Models;
using OpenPrismNode;
using OpenPrismNode.Core;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.Crypto;
using OpenPrismNode.Core.Models.DidDocument;
using OpenPrismNode.Core.Services;
using OpenPrismNode.Sync.Services;
using OpenPrismNode.Web;
using OpenPrismNode.Web.Components;
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
builder.Services.AddRazorComponents(o => o.DetailedErrors = builder.Environment.IsDevelopment())
    .AddInteractiveServerComponents();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // We want to forward the X-Forwarded-For and X-Forwarded-Proto headers
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // Optional but often recommended: if you do not know the exact proxies,
    // clear these so the middleware processes all forwarding headers.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

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
            []
        }
    });
});

var appSettingsSection = builder.Configuration.GetSection("AppSettings");
builder.Services.Configure<AppSettings>(appSettingsSection);
var appSettings = appSettingsSection.Get<AppSettings>();

// Set UI port values to their default if they're not specified
if (appSettings != null)
{
    if (appSettings.ApiHttpPortUi == 0)
    {
        appSettings.ApiHttpPortUi = appSettings.ApiHttpsPort;
    }
    
    if (appSettings.GrpcPortUi == 0)
    {
        appSettings.GrpcPortUi = appSettings.GrpcPort;
    }
}
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
    options.Listen(IPAddress.Any, appSettings!.ApiHttpsPort, listenOptions =>
    {
        // listenOptions.UseHttps(); // Enforce HTTPS
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });

    // Listen for gRPC services over HTTP/2 without TLS
    options.Listen(IPAddress.Any, appSettings.GrpcPort, listenOptions => { listenOptions.Protocols = HttpProtocols.Http2; });
});

builder.Services.AddGrpc(options => { options.EnableDetailedErrors = true; });
builder.Services.AddSingleton<IEcService, EcServiceBouncyCastle>();
builder.Services.AddSingleton<ISha256Service, Sha256ServiceBouncyCastle>();
builder.Services.AddScoped<INpgsqlConnectionFactory, NpgsqlConnectionFactory>();
builder.Services.AddScoped<BackgroundSyncService>();
builder.Services.AddScoped<ICardanoWalletService, CardanoWalletService>();
builder.Services.AddScoped<IIngestionService, IngestionService>();

// Register HTTP client for Blockfrost API
builder.Services.AddHttpClient("BlockfrostApi");

// Register Blockfrost API client
builder.Services.AddScoped<OpenPrismNode.Sync.Implementations.Blockfrost.BlockfrostApiClient>();

// Configure data source providers based on configuration
var syncDataSourceProvider = appSettings?.SyncDataSource?.Provider ?? "DbSync";

// Configuration is set up to use Blockfrost or DbSync based on appsettings.json

// Register the appropriate implementation based on configuration
if (syncDataSourceProvider.Equals("Blockfrost", StringComparison.OrdinalIgnoreCase))
{
    // Register transaction provider
    builder.Services.AddScoped<OpenPrismNode.Sync.Abstractions.ITransactionProvider, OpenPrismNode.Sync.Implementations.Blockfrost.BlockfrostTransactionProvider>();
    
    // Register API handlers
    builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<OpenPrismNode.Sync.Commands.ApiSync.GetApiBlockTip.GetApiBlockTipHandler>());
    
    // Register block provider that uses dedicated API handlers
    builder.Services.AddScoped<OpenPrismNode.Sync.Abstractions.IBlockProvider, OpenPrismNode.Sync.Implementations.Blockfrost.BlockfrostBlockProvider>();
}
else
{
    builder.Services.AddScoped<OpenPrismNode.Sync.Abstractions.ITransactionProvider, OpenPrismNode.Sync.Implementations.DbSync.DbSyncTransactionProvider>();
    builder.Services.AddScoped<OpenPrismNode.Sync.Abstractions.IBlockProvider, OpenPrismNode.Sync.Implementations.DbSync.DbSyncBlockProvider>();
}
builder.Services.AddSingleton<IWalletAddressCache>(new WalletAddressCache(appSettings!.WalletCacheSize));
builder.Services.AddSingleton<IStakeAddressCache>(new StakeAddressCache(appSettings.WalletCacheSize));
builder.Services.AddLazyCache();
builder.Services.AddHttpClient("LocalApi")
    .ConfigureHttpClient((serviceProvider, client) => { }).ConfigurePrimaryHttpMessageHandler(() =>
    {
        return new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) => true // <-- Accept all
        };
    });
builder.Services.AddHttpClient("CardanoWalletApi", client => { client.BaseAddress = new Uri($"{appSettings.CardanoWalletApiEndpoint}:{appSettings.CardanoWalletApiEndpointPort}/v2/"); });
builder.Services.AddHttpClient("Ingestion", client => { client.BaseAddress = appSettings.IngestionEndpoint; });
builder.Services.AddSingleton<BackgroundSyncService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<BackgroundSyncService>());
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies()));
builder.Services.AddDbContext<DataContext>(options =>
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
        // .EnableSensitiveDataLogging(true)
        .UseNpgsql(appSettings!.PrismLedger.PrismPostgresConnectionString)
        .ConfigureWarnings(p => p.Ignore(RelationalEventId.PendingModelChangesWarning))
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

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
        options.ClaimsIssuer = "OpenPrismNode";
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("User", policy => policy.RequireRole("Admin", "User"));
    options.AddPolicy("WalletUser", policy => policy.RequireRole("WalletUser"));
});

var app = builder.Build();

app.UseForwardedHeaders();

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGrpcService<OpenPrismNode.Web.Services.NodeService>();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.UseSwagger();
app.UseSwaggerUI();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DataContext>();
    dbContext.Database.Migrate(); // Will create and apply migrations if needed
}

app.Run();