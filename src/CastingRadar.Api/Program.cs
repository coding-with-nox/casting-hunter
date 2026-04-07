using CastingRadar.Api.Endpoints;
using CastingRadar.Infrastructure;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddEndpointsApiExplorer();

var usePostgres = builder.Configuration.GetValue<bool>("UsePostgres");
builder.Services.AddInfrastructure(builder.Configuration, usePostgres);

builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddHealthChecks();

var app = builder.Build();

// Apply migrations
await app.Services.MigrateAsync();

app.UseCors();
app.UseStaticFiles();

// API endpoints
app.MapCastingEndpoints();
app.MapSourceEndpoints();
app.MapProfileEndpoints();
app.MapStatsEndpoints();
app.MapHealthChecks("/health");

// SPA fallback (serves index.html for non-API routes)
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }
