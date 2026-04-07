using CastingRadar.Domain.Entities;
using CastingRadar.Domain.Enums;
using CastingRadar.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CastingRadar.Infrastructure.Scrapers.ItalianSources;

public class AttoriCastingScraper(IHttpClientFactory httpClientFactory, ILogger<AttoriCastingScraper> logger)
    : BaseScraper(httpClientFactory, logger)
{
    public override string SourceName => "AttoriCasting";
    public override SourceRegion Region => SourceRegion.Italy;
    protected override string HttpClientName => "Scraper";

    private const string BaseUrl = "https://www.attoricasting.it/casting-e-provini/";

    protected override async Task<IEnumerable<CastingCall>> ScrapeInternalAsync(ScraperFilter filter, CancellationToken ct)
    {
        var results = new List<CastingCall>();

        for (int page = 1; page <= 3; page++)
        {
            var url = page == 1 ? BaseUrl : $"{BaseUrl}page/{page}/";
            var doc = await LoadDocumentAsync(url, ct);

            var items = doc.QuerySelectorAll("article, .post, .entry");
            if (!items.Any()) break;

            foreach (var item in items)
            {
                var link = item.QuerySelector("a")?.GetAttribute("href");
                if (link is null) continue;

                var title = ParseText(item.QuerySelector("h2, h3, .entry-title")) ?? "Casting";
                var desc = ParseText(item.QuerySelector(".entry-summary, .excerpt, p")) ?? string.Empty;
                var location = ParseText(item.QuerySelector(".location, .citta"));

                results.Add(CastingCall.Create(
                    title: title,
                    description: desc,
                    sourceUrl: link,
                    sourceName: SourceName,
                    type: DetectType(title + desc),
                    region: Region,
                    location: location,
                    isPaid: desc.Contains("retribuit", StringComparison.OrdinalIgnoreCase) ||
                            title.Contains("retribuit", StringComparison.OrdinalIgnoreCase)));
            }

            await Task.Delay(2000, ct);
        }

        return results;
    }

    private static CastingType DetectType(string text)
    {
        if (text.Contains("film", StringComparison.OrdinalIgnoreCase)) return CastingType.Film;
        if (text.Contains("serie tv", StringComparison.OrdinalIgnoreCase) || text.Contains(" tv ", StringComparison.OrdinalIgnoreCase)) return CastingType.TV;
        if (text.Contains("teatro", StringComparison.OrdinalIgnoreCase)) return CastingType.Teatro;
        if (text.Contains("pubblicità", StringComparison.OrdinalIgnoreCase) || text.Contains("spot", StringComparison.OrdinalIgnoreCase)) return CastingType.Pubblicità;
        if (text.Contains("cortometraggio", StringComparison.OrdinalIgnoreCase) || text.Contains("corto", StringComparison.OrdinalIgnoreCase)) return CastingType.Cortometraggio;
        return CastingType.Altro;
    }
}
