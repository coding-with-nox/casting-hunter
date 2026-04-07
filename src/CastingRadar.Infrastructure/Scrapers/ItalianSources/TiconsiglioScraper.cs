using CastingRadar.Domain.Entities;
using CastingRadar.Domain.Enums;
using CastingRadar.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CastingRadar.Infrastructure.Scrapers.ItalianSources;

public class TiconsiglioScraper(IHttpClientFactory httpClientFactory, ILogger<TiconsiglioScraper> logger)
    : BaseScraper(httpClientFactory, logger)
{
    public override string SourceName => "Ticonsiglio";
    public override SourceRegion Region => SourceRegion.Italy;
    protected override string HttpClientName => "Scraper";

    private const string BaseUrl = "https://ticonsiglio.com/casting/";

    protected override async Task<IEnumerable<CastingCall>> ScrapeInternalAsync(ScraperFilter filter, CancellationToken ct)
    {
        var results = new List<CastingCall>();
        var doc = await LoadDocumentAsync(BaseUrl, ct);

        var articles = doc.QuerySelectorAll("article, .post, .entry, .casting-item, h2.entry-title a, .post-title a");

        // Try multiple selectors for article links
        var links = doc.QuerySelectorAll("article a[href], .post a[href], h2 a[href]")
            .Select(a => a.GetAttribute("href"))
            .Where(href => href is not null && href.Contains("casting"))
            .Distinct()
            .Take(20)
            .ToList();

        foreach (var link in links)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                var detailDoc = await LoadDocumentAsync(link!, ct);
                var title = ParseText(detailDoc.QuerySelector("h1.entry-title, h1.post-title, h1")) ?? "Casting";
                var desc = ParseText(detailDoc.QuerySelector(".entry-content, .post-content, article p")) ?? string.Empty;

                var call = CastingCall.Create(
                    title: title,
                    description: desc,
                    sourceUrl: link!,
                    sourceName: SourceName,
                    type: DetectType(title + desc),
                    region: Region,
                    isPaid: desc.Contains("retribuit", StringComparison.OrdinalIgnoreCase) ||
                            desc.Contains("pagat", StringComparison.OrdinalIgnoreCase));

                results.Add(call);
                await Task.Delay(2000, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to scrape detail page {Url}", link);
            }
        }

        return results;
    }

    private static CastingType DetectType(string text)
    {
        if (text.Contains("film", StringComparison.OrdinalIgnoreCase)) return CastingType.Film;
        if (text.Contains("serie tv", StringComparison.OrdinalIgnoreCase) || text.Contains("televisione", StringComparison.OrdinalIgnoreCase)) return CastingType.TV;
        if (text.Contains("teatro", StringComparison.OrdinalIgnoreCase)) return CastingType.Teatro;
        if (text.Contains("pubblicità", StringComparison.OrdinalIgnoreCase) || text.Contains("spot", StringComparison.OrdinalIgnoreCase)) return CastingType.Pubblicità;
        if (text.Contains("corto", StringComparison.OrdinalIgnoreCase) || text.Contains("cortometraggio", StringComparison.OrdinalIgnoreCase)) return CastingType.Cortometraggio;
        return CastingType.Altro;
    }
}
