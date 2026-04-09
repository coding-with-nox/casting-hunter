using AngleSharp.Dom;
using CastingRadar.Application.Interfaces;
using CastingRadar.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CastingRadar.Infrastructure.Scrapers.Bandi;

public class SanCarloBandoScraper(IHttpClientFactory httpClientFactory, ILogger<SanCarloBandoScraper> logger)
    : BaseBandoScraper(httpClientFactory, logger)
{
    private static readonly string[] PageUrls =
    [
        "https://www.teatrosancarlo.it/it/teatrosancarlo/concorsi-audizioni.html",
        "https://www.teatrosancarlo.it/it/teatrosancarlo/lavora-con-noi.html",
        "https://www.teatrosancarlo.it/",
    ];

    private static readonly string[] PositiveKeywords =
    [
        "audizione", "concorso", "selezione", "bando", "coro", "orchestra",
        "danzatore", "ballerino", "cantante", "soprano", "tenore"
    ];

    public override string SourceName => "Teatro di San Carlo";

    protected override async Task<IEnumerable<ScrapedBandoItem>> ScrapeInternalAsync(BandoSource source, CancellationToken ct)
    {
        foreach (var pageUrl in PageUrls)
        {
            try
            {
                var doc = await LoadDocumentAsync(pageUrl, ct);
                var candidates = doc.QuerySelectorAll("a[href]")
                    .Select(link => new
                    {
                        Title = CleanText(link.TextContent),
                        Url = TryAbsoluteUrl(pageUrl, link.GetAttribute("href"))
                    })
                    .Where(item => !string.IsNullOrWhiteSpace(item.Title) && !string.IsNullOrWhiteSpace(item.Url))
                    .Where(item => PositiveKeywords.Any(kw => item.Title.Contains(kw, StringComparison.OrdinalIgnoreCase)))
                    .DistinctBy(item => item.Url, StringComparer.OrdinalIgnoreCase)
                    .Take(10)
                    .ToList();

                if (candidates.Count == 0) continue;

                var results = new List<ScrapedBandoItem>();
                foreach (var candidate in candidates)
                {
                    var bodyText = candidate.Title;
                    DateTime? deadline = null;
                    string? appUrl = null;
                    try
                    {
                        var detail = await LoadDocumentAsync(candidate.Url!, ct);
                        var detailText = CleanText(detail.QuerySelector("main, article, .content, body")?.TextContent);
                        if (!string.IsNullOrWhiteSpace(detailText)) bodyText = detailText;
                        deadline = ExtractItalianDateFromText(bodyText);
                        appUrl = detail.QuerySelectorAll("a[href]")
                            .FirstOrDefault(a => CleanText(a.TextContent).Contains("domanda", StringComparison.OrdinalIgnoreCase)
                                || CleanText(a.TextContent).Contains("iscri", StringComparison.OrdinalIgnoreCase))
                            ?.GetAttribute("href");
                    }
                    catch { /* keep fallback */ }

                    results.Add(new ScrapedBandoItem(
                        Title: candidate.Title,
                        SourceUrl: candidate.Url!,
                        BodyText: bodyText,
                        Deadline: deadline,
                        ApplicationUrl: TryAbsoluteUrl(pageUrl, appUrl),
                        IssuerName: "Fondazione Teatro di San Carlo"));
                }

                if (results.Count > 0)
                    return results.DistinctBy(r => r.SourceUrl, StringComparer.OrdinalIgnoreCase).ToList();
            }
            catch { /* try next page */ }
        }

        return [];
    }
}
