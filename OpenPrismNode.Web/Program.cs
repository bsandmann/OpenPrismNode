using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenPrismNode.Core;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.Crypto;
using OpenPrismNode.Core.Models;
using OpenPrismNode.Core.Services;
using OpenPrismNode.Sync.Services;
using OpenPrismNode.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var appSettingsSection = builder.Configuration.GetSection("AppSettings");
builder.Services.Configure<AppSettings>(appSettingsSection);
var appSettings = appSettingsSection.Get<AppSettings>();
builder.Services.AddSingleton<IEcService, EcServiceBouncyCastle>();
builder.Services.AddSingleton<ISha256Service, Sha256ServiceBouncyCastle>();
builder.Services.AddScoped<INpgsqlConnectionFactory, NpgsqlConnectionFactory>();
builder.Services.AddScoped<BackgroundSyncService>();
builder.Services.AddSingleton<IWalletAddressCache>(new WalletAddressCache(appSettings!.WalletCacheSize));
builder.Services.AddSingleton<IStakeAddressCache>(new StakeAddressCache(appSettings.WalletCacheSize));
builder.Services.AddLazyCache();
builder.Services.AddHostedService<BackgroundSyncService>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies()));
builder.Services.AddDbContext<DataContext>(options =>
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
        .EnableSensitiveDataLogging(true)
        .UseNpgsql(appSettings!.PrismNetwork.PrismPostgresConnectionString)
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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();