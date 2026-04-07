using CastingRadar.Domain.Entities;
using CastingRadar.Domain.Enums;
using CastingRadar.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CastingRadar.Infrastructure.Scrapers.ItalianSources;

public class IMoviezScraper(IHttpClientFactory httpClientFactory, ILogger<IMoviezScraper> logger)
    : BaseScraper(httpClientFactory, logger)
{
    public override string SourceName => "iMoviez";
    public override SourceRegion Region => SourceRegion.Italy;
    protected override string HttpClientName => "Scraper";

    private const string BaseUrl = "https://casting.imoviez.it/";

    protected override async Task<IEnumerable<CastingCall>> ScrapeInternalAsync(ScraperFilter filter, CancellationToken ct)
    {
        var results = new List<CastingCall>();
        var doc = await LoadDocumentAsync(BaseUrl, ct);

        var items = doc.QuerySelectorAll(".casting-item, .annuncio, article, .card, .list-item");

        foreach (var item in items.Take(20))
        {
            var titleEl = item.QuerySelector("h2, h3, .title, .titolo");
            var title = ParseText(titleEl) ?? "Casting";
            var link = item.QuerySelector("a")?.GetAttribute("href");
            if (link is null) continue;
            if (!link.StartsWith("http")) link = BaseUrl.TrimEnd('/') + "/" + link.TrimStart('/');

            var desc = ParseText(item.QuerySelector("p, .description, .testo")) ?? string.Empty;
            var location = ParseText(item.QuerySelector(".location, .luogo, .citta"));

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
        if (text.Contains("serie", StringComparison.OrdinalIgnoreCase) || text.Contains("tv", StringComparison.OrdinalIgnoreCase)) return CastingType.TV;
        if (text.Contains("teatro", StringComparison.OrdinalIgnoreCase)) return CastingType.Teatro;
        if (text.Contains("spot", StringComparison.OrdinalIgnoreCase) || text.Contains("pubblicità", StringComparison.OrdinalIgnoreCase)) return CastingType.Pubblicità;
        if (text.Contains("corto", StringComparison.OrdinalIgnoreCase)) return CastingType.Cortometraggio;
        return CastingType.Altro;
    }
}
