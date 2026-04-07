using CastingRadar.Domain.Entities;
using CastingRadar.Domain.Enums;
using CastingRadar.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CastingRadar.Infrastructure.Scrapers.ItalianSources;

public class CastingEProviniScraper(IHttpClientFactory httpClientFactory, ILogger<CastingEProviniScraper> logger)
    : BaseScraper(httpClientFactory, logger)
{
    public override string SourceName => "CastingEProvini";
    public override SourceRegion Region => SourceRegion.Italy;
    protected override string HttpClientName => "Scraper";

    private const string BaseUrl = "https://www.castingeprovini.com/";

    protected override async Task<IEnumerable<CastingCall>> ScrapeInternalAsync(ScraperFilter filter, CancellationToken ct)
    {
        var results = new List<CastingCall>();
        var doc = await LoadDocumentAsync(BaseUrl, ct);

        var items = doc.QuerySelectorAll("article, .post, .entry, .news-item");

        foreach (var item in items.Take(20))
        {
            var link = item.QuerySelector("a[href]")?.GetAttribute("href");
            if (link is null) continue;
            if (!link.StartsWith("http")) link = BaseUrl.TrimEnd('/') + "/" + link.TrimStart('/');

            var title = ParseText(item.QuerySelector("h2, h3, .entry-title, .post-title")) ?? "Casting";
            var desc = ParseText(item.QuerySelector(".entry-summary, .excerpt, p")) ?? string.Empty;
            var location = ParseText(item.QuerySelector(".location, .citta, .luogo"));

            results.Add(CastingCall.Create(
                title: title,
                description: desc,
                sourceUrl: link,
                sourceName: SourceName,
                type: DetectType(title + desc),
                region: Region,
                location: location,
                isPaid: desc.Contains("retribuit", StringComparison.OrdinalIgnoreCase)));

            await Task.Delay(2000, ct);
        }

        return results;
    }

    private static CastingType DetectType(string text)
    {
        if (text.Contains("film", StringComparison.OrdinalIgnoreCase)) return CastingType.Film;
        if (text.Contains("serie tv", StringComparison.OrdinalIgnoreCase) || text.Contains("televisione", StringComparison.OrdinalIgnoreCase)) return CastingType.TV;
        if (text.Contains("teatro", StringComparison.OrdinalIgnoreCase)) return CastingType.Teatro;
        if (text.Contains("pubblicità", StringComparison.OrdinalIgnoreCase) || text.Contains("spot", StringComparison.OrdinalIgnoreCase)) return CastingType.Pubblicità;
        if (text.Contains("cortometraggio", StringComparison.OrdinalIgnoreCase) || text.Contains("corto", StringComparison.OrdinalIgnoreCase)) return CastingType.Cortometraggio;
        return CastingType.Altro;
    }
}
