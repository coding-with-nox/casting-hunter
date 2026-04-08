using CastingRadar.Api.Endpoints;
using CastingRadar.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using System.Threading.RateLimiting;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/api-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

builder.Services.AddEndpointsApiExplorer();

var usePostgres = builder.Configuration.GetValue<bool>("UsePostgres");
builder.Services.AddInfrastructure(builder.Configuration, usePostgres);

// CORS: restrict to known origins (not wildcard)
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173", "http://localhost:5050"];

builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .WithMethods("GET", "POST", "PUT", "DELETE")));

// Rate limiting: protect heavy endpoints from abuse
builder.Services.AddRateLimiter(options =>
{
    // Scrape-all: max 3 requests per 10 minutes per IP
    options.AddFixedWindowLimiter("scrape", cfg =>
    {
        cfg.PermitLimit = 3;
        cfg.Window = TimeSpan.FromMinutes(10);
        cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        cfg.QueueLimit = 0;
    });

    // General API: 60 req/min per IP
    options.AddFixedWindowLimiter("api", cfg =>
    {
        cfg.PermitLimit = 60;
        cfg.Window = TimeSpan.FromMinutes(1);
        cfg.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        cfg.QueueLimit = 5;
    });

    options.RejectionStatusCode = 429;
});

builder.Services.AddHealthChecks();

var app = builder.Build();

// Apply migrations
await app.Services.MigrateAsync();

// Security headers middleware
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Content-Security-Policy"] =
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; connect-src 'self'";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    // Remove server fingerprinting header
    headers.Remove("Server");
    await next();
});

app.UseCors();
app.UseRateLimiter();
app.UseStaticFiles();

// API endpoints (general rate limit)
app.MapCastingEndpoints();
app.MapSourceEndpoints();
app.MapProfileEndpoints();
app.MapStatsEndpoints();
app.MapHealthChecks("/health");

// SPA fallback
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }
