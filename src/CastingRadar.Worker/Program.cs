using CastingRadar.Infrastructure;
using CastingRadar.Worker;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/worker-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSerilog();

var usePostgres = builder.Configuration.GetValue<bool>("UsePostgres");
builder.Services.AddInfrastructure(builder.Configuration, usePostgres);
builder.Services.AddHostedService<ScrapeJob>();

var host = builder.Build();

// Apply migrations on startup
await host.Services.MigrateAsync();

host.Run();
