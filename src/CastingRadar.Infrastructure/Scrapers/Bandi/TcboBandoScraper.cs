using AngleSharp.Dom;
using CastingRadar.Application.Interfaces;
using CastingRadar.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CastingRadar.Infrastructure.Scrapers.Bandi;

public class TcboBandoScraper(IHttpClientFactory httpClientFactory, ILogger<TcboBandoScraper> logger)
    : BaseBandoScraper(httpClientFactory, logger)
{
    private static readonly string[] PageUrls =
    [
        "https://www.tcbo.it/il-teatro/lavora-con-noi/",
        "https://www.tcbo.it/",
    ];

    public override string SourceName => "Teatro Comunale di Bologna";

    protected override async Task<IEnumerable<ScrapedBandoItem>> ScrapeInternalAsync(BandoSource source, CancellationToken ct)
    {
        var results = new List<ScrapedBandoItem>();

        foreach (var url in PageUrls)
        {
            try
            {
                var doc = await LoadDocumentAsync(url, ct);
                var candidates = doc.QuerySelectorAll("a[href]")
                    .Select(link => new
                    {
                        Title = CleanText(link.TextContent),
                        Url = TryAbsoluteUrl(url, link.GetAttribute("href"))
                    })
                    .Where(item => !string.IsNullOrWhiteSpace(item.Title) && !string.IsNullOrWhiteSpace(item.Url))
                    .Where(item =>
                        item.Title.Contains("Audizione", StringComparison.OrdinalIgnoreCase) ||
                        item.Title.Contains("Selezione", StringComparison.OrdinalIgnoreCase) ||
                        item.Title.Contains("Concorso", StringComparison.OrdinalIgnoreCase) ||
                        item.Title.Contains("Bando", StringComparison.OrdinalIgnoreCase) ||
                        item.Title.Contains("Artisti del coro", StringComparison.OrdinalIgnoreCase) ||
                        item.Title.Contains("Maestro collaboratore", StringComparison.OrdinalIgnoreCase))
                    .Where(item =>
                        !item.Title.Contains("ESITO", StringComparison.OrdinalIgnoreCase) &&
                        !item.Title.Contains("Programma d'esame", StringComparison.OrdinalIgnoreCase))
                    .DistinctBy(item => item.Url, StringComparer.OrdinalIgnoreCase)
                    .Take(15)
                    .ToList();

                foreach (var candidate in candidates)
                {
                    var bodyText = candidate.Title;
                    DateTime? deadline = null;
                    try
                    {
                        var detail = await LoadDocumentAsync(candidate.Url!, ct);
                        var detailText = CleanText(detail.QuerySelector("main, article, .content, body")?.TextContent);
                        if (!string.IsNullOrWhiteSpace(detailText)) bodyText = detailText;
                        deadline = ExtractItalianDateFromText(bodyText);
                    }
                    catch { /* keep title fallback */ }

                    results.Add(new ScrapedBandoItem(
                        Title: candidate.Title,
                        SourceUrl: candidate.Url!,
                        BodyText: bodyText,
                        Deadline: deadline,
                        IssuerName: "Fondazione Teatro Comunale di Bologna"));
                }

                if (results.Count > 0) break;
            }
            catch { /* try next url */ }
        }

        return results
            .DistinctBy(r => r.SourceUrl, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
