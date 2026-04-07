using CastingRadar.Application.UseCases.ScrapeAllSources;
using CastingRadar.Domain.ValueObjects;
using CastingRadar.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CastingRadar.Worker;

public class ScrapeJob(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<ScrapeJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalHours = configuration.GetValue<int>("CastingRadar:ScrapingIntervalHours", 6);
        var interval = TimeSpan.FromHours(intervalHours);

        logger.LogInformation("ScrapeJob started. Interval: {Hours}h", intervalHours);

        // Run immediately on startup
        await RunScrapeAsync(stoppingToken);

        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunScrapeAsync(stoppingToken);
        }
    }

    private async Task RunScrapeAsync(CancellationToken ct)
    {
        logger.LogInformation("Starting scheduled scrape run...");
        try
        {
            using var scope = scopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<ScrapeAllSourcesHandler>();
            var filter = BuildFilter();
            var result = await handler.HandleAsync(filter, ct);
            logger.LogInformation("Scrape complete: Found={Found}, New={New}", result.TotalFound, result.TotalNew);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogError(ex, "Scrape run failed");
        }
    }

    private ScraperFilter BuildFilter()
    {
        var section = configuration.GetSection("CastingRadar:DefaultFilter");
        var onlyPaid = section.GetValue<bool>("OnlyPaid");
        var gender = section.GetValue<string>("GenderFilter");
        return ScraperFilter.Default with { OnlyPaid = onlyPaid, GenderFilter = gender };
    }
}
