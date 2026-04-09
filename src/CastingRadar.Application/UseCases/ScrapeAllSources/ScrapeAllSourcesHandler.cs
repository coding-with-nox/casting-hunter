using CastingRadar.Application.Interfaces;
using CastingRadar.Domain.Entities;
using CastingRadar.Domain.Enums;
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

        foreach (var scraper in scrapers)
        {
            var source = await sourceRepository.GetByNameAsync(scraper.SourceName, ct)
                ?? Source.Create(scraper.SourceName, scraper.Region);

            // Respect DB toggle — skip if disabled (default: enabled)
            if (!source.IsEnabled) continue;

            try
            {
                logger.LogInformation("Scraping {Source}...", scraper.SourceName);
                var calls = (await scraper.ScrapeAsync(filter, ct)).ToList();
                totalFound += calls.Count;

                var seenHashes = new HashSet<string>();
                var newCalls = new List<CastingCall>();
                foreach (var call in calls)
                {
                    if (seenHashes.Add(call.ContentHash)
                        && !await castingRepository.ExistsByHashAsync(call.ContentHash, ct))
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

        // Scrape generic sources (DB-only, with URL, no dedicated strategy)
        var registeredNames = scrapers.Select(s => s.SourceName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var allSources = await sourceRepository.GetAllAsync(ct);
        var genericSources = allSources
            .Where(s => s.IsEnabled && s.Url is not null && !registeredNames.Contains(s.Name))
            .ToList();

        foreach (var source in genericSources)
        {
            try
            {
                logger.LogInformation("Scraping generic source {Source} at {Url}...", source.Name, source.Url);
                var calls = (await ScrapeGenericAsync(source, filter, ct)).ToList();
                totalFound += calls.Count;

                var seenHashes = new HashSet<string>();
                var newCalls = new List<CastingCall>();
                foreach (var call in calls)
                {
                    if (seenHashes.Add(call.ContentHash)
                        && !await castingRepository.ExistsByHashAsync(call.ContentHash, ct))
                        newCalls.Add(call);
                }

                if (newCalls.Count > 0)
                {
                    await castingRepository.AddRangeAsync(newCalls, ct);
                    totalNew += newCalls.Count;
                }

                source.RecordSuccess();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Generic scraper failed for {Source}", source.Name);
                source.RecordError();
            }

            await sourceRepository.UpsertAsync(source, ct);
        }

        logger.LogInformation("Scrape complete. Found={Found}, New={New}", totalFound, totalNew);
        return new ScrapeResult(totalFound, totalNew);
    }

    private static async Task<IEnumerable<CastingCall>> ScrapeGenericAsync(
        Source source, ScraperFilter filter, CancellationToken ct)
    {
        using var http = new System.Net.Http.HttpClient();
        http.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120 Safari/537.36");

        var html = await http.GetStringAsync(source.Url!, ct);
        var results = new List<CastingCall>();
        var baseUri = new Uri(source.Url!);

        // Extract links + surrounding text via regex (no external dependency in Application layer)
        var linkPattern = System.Text.RegularExpressions.Regex.Matches(html,
            @"<a\s[^>]*href=[""']([^""']+)[""'][^>]*>\s*<(?:h[1-6]|strong|span)[^>]*>([^<]{5,120})</(?:h[1-6]|strong|span)>",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        foreach (System.Text.RegularExpressions.Match m in linkPattern.Cast<System.Text.RegularExpressions.Match>().Take(20))
        {
            var rawLink = m.Groups[1].Value;
            var title = System.Net.WebUtility.HtmlDecode(m.Groups[2].Value).Trim();
            if (string.IsNullOrWhiteSpace(title)) continue;

            var link = rawLink.StartsWith("http") ? rawLink : new Uri(baseUri, rawLink).ToString();

            results.Add(CastingCall.Create(
                title: title,
                description: string.Empty,
                sourceUrl: link,
                sourceName: source.Name,
                type: CastingType.Altro,
                region: source.Region));
        }

        return results;
    }
}

public record ScrapeResult(int TotalFound, int TotalNew);
