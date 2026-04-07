using CastingRadar.Application.Interfaces;
using CastingRadar.Domain.Entities;
using CastingRadar.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CastingRadar.Application.UseCases.ScrapeAllSources;

public class ScrapeAllSourcesHandler(
    IEnumerable<ICastingScraperStrategy> scrapers,
    ICastingRepository castingRepository,
    ISourceRepository sourceRepository,
    INotificationService notificationService,
    ILogger<ScrapeAllSourcesHandler> logger)
{
    public async Task<ScrapeResult> HandleAsync(ScraperFilter filter, CancellationToken ct = default)
    {
        int totalFound = 0, totalNew = 0;
        int notificationsSent = 0;
        const int maxNotifications = 10;

        foreach (var scraper in scrapers.Where(s => s.IsEnabled))
        {
            var source = await sourceRepository.GetByNameAsync(scraper.SourceName, ct)
                ?? Source.Create(scraper.SourceName, scraper.Region);

            try
            {
                logger.LogInformation("Scraping {Source}...", scraper.SourceName);
                var calls = (await scraper.ScrapeAsync(filter, ct)).ToList();
                totalFound += calls.Count;

                var newCalls = new List<CastingCall>();
                foreach (var call in calls)
                {
                    if (!await castingRepository.ExistsByHashAsync(call.ContentHash, ct))
                        newCalls.Add(call);
                }

                if (newCalls.Count > 0)
                {
                    await castingRepository.AddRangeAsync(newCalls, ct);
                    totalNew += newCalls.Count;
                }

                logger.LogInformation("{Source}: found={Found}, new={New}", scraper.SourceName, calls.Count, newCalls.Count);

                // Send notifications for new calls (rate-limited)
                foreach (var call in newCalls)
                {
                    if (notificationsSent >= maxNotifications) break;
                    try
                    {
                        await notificationService.SendAsync(call, ct);
                        notificationsSent++;
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to send notification for casting {Id}", call.Id);
                    }
                }

                source.RecordSuccess();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Scraper {Source} failed", scraper.SourceName);
                source.RecordError();
            }

            await sourceRepository.UpsertAsync(source, ct);
        }

        logger.LogInformation("Scrape complete. Found={Found}, New={New}", totalFound, totalNew);
        return new ScrapeResult(totalFound, totalNew);
    }
}

public record ScrapeResult(int TotalFound, int TotalNew);
