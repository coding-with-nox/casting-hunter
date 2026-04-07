// TODO: iCasting.it requires a membership for full access.
// This scraper only accesses the public portion of the site.
// Upgrade to the official API if/when it becomes available.
using CastingRadar.Domain.Entities;
using CastingRadar.Domain.Enums;
using CastingRadar.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CastingRadar.Infrastructure.Scrapers.ItalianSources;

public class ICastingScraper(IHttpClientFactory httpClientFactory, ILogger<ICastingScraper> logger)
    : BaseScraper(httpClientFactory, logger)
{
    public override string SourceName => "iCasting";
    public override SourceRegion Region => SourceRegion.Italy;
    protected override string HttpClientName => "Scraper";

    private const string BaseUrl = "https://www.icasting.it/casting";

    protected override async Task<IEnumerable<CastingCall>> ScrapeInternalAsync(ScraperFilter filter, CancellationToken ct)
    {
        var results = new List<CastingCall>();
        var doc = await LoadDocumentAsync(BaseUrl, ct);

        // Only public listings visible without authentication
        var items = doc.QuerySelectorAll(".casting-card, .job-item, article, .listing-item");

        foreach (var item in items.Take(15))
        {
            var link = item.QuerySelector("a")?.GetAttribute("href");
            if (link is null) continue;
            if (!link.StartsWith("http")) link = "https://www.icasting.it" + link;

            var title = ParseText(item.QuerySelector("h2, h3, .title")) ?? "Casting";
            var desc = ParseText(item.QuerySelector("p, .description")) ?? string.Empty;

            results.Add(CastingCall.Create(
                title: title,
                description: desc,
                sourceUrl: link,
                sourceName: SourceName,
                type: CastingType.Altro,
                region: Region,
                isPaid: false));

            await Task.Delay(2000, ct);
        }

        return results;
    }
}
