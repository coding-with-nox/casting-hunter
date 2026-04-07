using CastingRadar.Domain.Entities;
using CastingRadar.Domain.Enums;
using CastingRadar.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CastingRadar.Infrastructure.Scrapers.InternationalSources;

public class MandyScraper(IHttpClientFactory httpClientFactory, ILogger<MandyScraper> logger)
    : BaseScraper(httpClientFactory, logger)
{
    public override string SourceName => "Mandy";
    public override SourceRegion Region => SourceRegion.Europe;
    protected override string HttpClientName => "Scraper";

    private const string BaseUrl = "https://www.mandy.com/film/job-listings?country=Italy";

    protected override async Task<IEnumerable<CastingCall>> ScrapeInternalAsync(ScraperFilter filter, CancellationToken ct)
    {
        var results = new List<CastingCall>();
        var doc = await LoadDocumentAsync(BaseUrl, ct);

        var items = doc.QuerySelectorAll(".job-listing, .listing-item, article, .job-card");

        foreach (var item in items.Take(15))
        {
            var link = item.QuerySelector("a[href]")?.GetAttribute("href");
            if (link is null) continue;
            if (!link.StartsWith("http")) link = "https://www.mandy.com" + link;

            var title = ParseText(item.QuerySelector("h2, h3, .job-title, .title")) ?? "Job Listing";
            var desc = ParseText(item.QuerySelector("p, .description, .summary")) ?? string.Empty;
            var location = ParseText(item.QuerySelector(".location, .country"));

            // Filter for Italy/Europe only
            var locationText = location ?? string.Empty;
            if (!locationText.Contains("Italy", StringComparison.OrdinalIgnoreCase) &&
                !locationText.Contains("Italia", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(locationText))
                continue;

            results.Add(CastingCall.Create(
                title: title,
                description: desc,
                sourceUrl: link,
                sourceName: SourceName,
                type: DetectType(title + desc),
                region: Region,
                location: location,
                isPaid: true)); // Mandy typically lists paid work

            await Task.Delay(2000, ct);
        }

        return results;
    }

    private static CastingType DetectType(string text)
    {
        if (text.Contains("film", StringComparison.OrdinalIgnoreCase)) return CastingType.Film;
        if (text.Contains("tv", StringComparison.OrdinalIgnoreCase) || text.Contains("series", StringComparison.OrdinalIgnoreCase)) return CastingType.TV;
        if (text.Contains("theatre", StringComparison.OrdinalIgnoreCase) || text.Contains("stage", StringComparison.OrdinalIgnoreCase)) return CastingType.Teatro;
        if (text.Contains("commercial", StringComparison.OrdinalIgnoreCase) || text.Contains("advert", StringComparison.OrdinalIgnoreCase)) return CastingType.Pubblicità;
        if (text.Contains("short", StringComparison.OrdinalIgnoreCase)) return CastingType.Cortometraggio;
        return CastingType.Internazionale;
    }
}
