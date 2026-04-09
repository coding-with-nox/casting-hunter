using AngleSharp.Dom;
using CastingRadar.Application.Interfaces;
using CastingRadar.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CastingRadar.Infrastructure.Scrapers.Bandi;

public class TeatroStabileTorinoBandoScraper(IHttpClientFactory httpClientFactory, ILogger<TeatroStabileTorinoBandoScraper> logger)
    : BaseBandoScraper(httpClientFactory, logger)
{
    private static readonly string[] PageUrls =
    [
        "https://www.teatrostabiletorino.it/scuola-per-attori/",
        "https://www.teatrostabiletorino.it/lavora-con-noi/",
        "https://www.teatrostabiletorino.it/audizioni/",
        "https://www.teatrostabiletorino.it/",
    ];

    private static readonly string[] Keywords =
    [
        "bando", "audizione", "selezione", "concorso", "attori", "corso per attori",
        "ammissione", "iscrizione", "formazione"
    ];

    public override string SourceName => "Teatro Stabile di Torino";

    protected override async Task<IEnumerable<ScrapedBandoItem>> ScrapeInternalAsync(BandoSource source, CancellationToken ct)
    {
        var results = new List<ScrapedBandoItem>();

        foreach (var pageUrl in PageUrls)
        {
            try
            {
                var doc = await LoadDocumentAsync(pageUrl, ct);
                var bodyText = CleanText(doc.QuerySelector("main, article, .content")?.TextContent ?? doc.Body?.TextContent);

                var candidates = doc.QuerySelectorAll("a[href]")
                    .Select(link => new
                    {
                        Title = CleanText(link.TextContent),
                        Url = TryAbsoluteUrl(pageUrl, link.GetAttribute("href"))
                    })
                    .Where(item => !string.IsNullOrWhiteSpace(item.Title) && !string.IsNullOrWhiteSpace(item.Url))
                    .Where(item => Keywords.Any(kw => item.Title.Contains(kw, StringComparison.OrdinalIgnoreCase)))
                    .DistinctBy(item => item.Url, StringComparer.OrdinalIgnoreCase)
                    .Take(8)
                    .ToList();

                foreach (var candidate in candidates)
                {
                    var detailText = candidate.Title;
                    DateTime? deadline = null;
                    try
                    {
                        var detail = await LoadDocumentAsync(candidate.Url!, ct);
                        var dt = CleanText(detail.QuerySelector("main, article, .content, body")?.TextContent);
                        if (!string.IsNullOrWhiteSpace(dt)) detailText = dt;
                        deadline = ExtractItalianDateFromText(detailText);
                    }
                    catch { /* keep fallback */ }

                    results.Add(new ScrapedBandoItem(
                        Title: candidate.Title,
                        SourceUrl: candidate.Url!,
                        BodyText: detailText,
                        Deadline: deadline,
                        IssuerName: "Teatro Stabile di Torino"));
                }

                // Even if no sub-links, emit the page itself if it looks like a bando page
                if (candidates.Count == 0 && Keywords.Any(kw => bodyText?.Contains(kw, StringComparison.OrdinalIgnoreCase) == true))
                {
                    results.Add(new ScrapedBandoItem(
                        Title: CleanText(doc.QuerySelector("h1, h2")?.TextContent) ?? "Bando Teatro Stabile Torino",
                        SourceUrl: pageUrl,
                        BodyText: bodyText ?? string.Empty,
                        Deadline: ExtractItalianDateFromText(bodyText),
                        IssuerName: "Teatro Stabile di Torino"));
                }
            }
            catch { /* try next */ }
        }

        return results
            .DistinctBy(r => r.SourceUrl, StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();
    }
}
