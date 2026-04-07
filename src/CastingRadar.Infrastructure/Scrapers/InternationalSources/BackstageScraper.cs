using CastingRadar.Domain.Entities;
using CastingRadar.Domain.Enums;
using CastingRadar.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CastingRadar.Infrastructure.Scrapers.InternationalSources;

public class BackstageScraper(IHttpClientFactory httpClientFactory, ILogger<BackstageScraper> logger)
    : BaseScraper(httpClientFactory, logger)
{
    public override string SourceName => "Backstage";
    public override SourceRegion Region => SourceRegion.International;
    protected override string HttpClientName => "Scraper";

    private const string BaseUrl = "https://www.backstage.com/casting-calls/";

    protected override async Task<IEnumerable<CastingCall>> ScrapeInternalAsync(ScraperFilter filter, CancellationToken ct)
    {
        var results = new List<CastingCall>();
        var doc = await LoadDocumentAsync(BaseUrl, ct);

        var items = doc.QuerySelectorAll(".listing-card, .casting-card, article, .job-item");

        foreach (var item in items.Take(15))
        {
            var link = item.QuerySelector("a[href]")?.GetAttribute("href");
            if (link is null) continue;
            if (!link.StartsWith("http")) link = "https://www.backstage.com" + link;

            var title = ParseText(item.QuerySelector("h2, h3, .title")) ?? "Casting Call";
            var desc = ParseText(item.QuerySelector("p, .description")) ?? string.Empty;
            var location = ParseText(item.QuerySelector(".location"));

            // Parse deadline if present
            DateTime? deadline = null;
            var deadlineText = ParseText(item.QuerySelector(".deadline, .expires, .date"));
            if (deadlineText is not null && DateTime.TryParse(deadlineText, out var parsed))
                deadline = parsed;

            results.Add(CastingCall.Create(
                title: title,
                description: desc,
                sourceUrl: link,
                sourceName: SourceName,
                type: DetectType(title + desc),
                region: Region,
                location: location,
                deadline: deadline,
                isPaid: desc.Contains("paid", StringComparison.OrdinalIgnoreCase)));

            await Task.Delay(2000, ct);
        }

        return results;
    }

    private static CastingType DetectType(string text)
    {
        if (text.Contains("film", StringComparison.OrdinalIgnoreCase)) return CastingType.Film;
        if (text.Contains("tv", StringComparison.OrdinalIgnoreCase) || text.Contains("television", StringComparison.OrdinalIgnoreCase)) return CastingType.TV;
        if (text.Contains("theater", StringComparison.OrdinalIgnoreCase) || text.Contains("theatre", StringComparison.OrdinalIgnoreCase)) return CastingType.Teatro;
        if (text.Contains("commercial", StringComparison.OrdinalIgnoreCase)) return CastingType.Pubblicità;
        if (text.Contains("short film", StringComparison.OrdinalIgnoreCase)) return CastingType.Cortometraggio;
        return CastingType.Internazionale;
    }
}
