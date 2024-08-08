using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenPrismNode.Core;
using OpenPrismNode.Core.Common;
using OpenPrismNode.Core.Crypto;
using OpenPrismNode.Core.Models;
using OpenPrismNode.Sync.Services;
using OpenPrismNode.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.AddSingleton<IEcService, EcServiceBouncyCastle>();
builder.Services.AddSingleton<ISha256Service, Sha256ServiceBouncyCastle>();
builder.Services.AddScoped<INpgsqlConnectionFactory, NpgsqlConnectionFactory>();
builder.Services.AddScoped<BackgroundSyncService>();
builder.Services.AddHostedService<BackgroundSyncService>();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies()));
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("CredentialBadgesDatabase")));



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