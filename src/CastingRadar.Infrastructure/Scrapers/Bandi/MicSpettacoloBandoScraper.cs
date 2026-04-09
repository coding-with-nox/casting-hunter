using AngleSharp.Dom;
using CastingRadar.Application.Interfaces;
using CastingRadar.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CastingRadar.Infrastructure.Scrapers.Bandi;

public class MicSpettacoloBandoScraper(IHttpClientFactory httpClientFactory, ILogger<MicSpettacoloBandoScraper> logger)
    : BaseBandoScraper(httpClientFactory, logger)
{
    public override string SourceName => "MiC Spettacolo";

    protected override async Task<IEnumerable<ScrapedBandoItem>> ScrapeInternalAsync(BandoSource source, CancellationToken ct)
    {
        var doc = await LoadDocumentAsync(source.BaseUrl, ct);
        var links = doc.QuerySelectorAll("a[href]")
            .Select(link => new
            {
                Title = CleanText(link.TextContent),
                Url = TryAbsoluteUrl(source.BaseUrl, link.GetAttribute("href"))
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Title) && !string.IsNullOrWhiteSpace(item.Url))
            .Where(item => item.Title.Contains("bando", StringComparison.OrdinalIgnoreCase)
                || item.Title.Contains("avviso", StringComparison.OrdinalIgnoreCase)
                || item.Title.Contains("contribut", StringComparison.OrdinalIgnoreCase)
                || item.Title.Contains("fondazioni", StringComparison.OrdinalIgnoreCase)
                || item.Title.Contains("spettacolo", StringComparison.OrdinalIgnoreCase))
            .DistinctBy(item => item.Url, StringComparer.OrdinalIgnoreCase)
            .Take(18)
            .ToList();

        var results = new List<ScrapedBandoItem>();
        foreach (var link in links)
        {
            var bodyText = link.Title;
            DateTime? deadline = null;
            try
            {
                var detail = await LoadDocumentAsync(link.Url!, ct);
                bodyText = CleanText(detail.QuerySelector("main, article, .entry-content, .content, body")?.TextContent);
                deadline = ExtractItalianDateFromText(bodyText);
            }
            catch
            {
                // Keep the link title as body fallback.
            }

            results.Add(new ScrapedBandoItem(
                Title: link.Title,
                SourceUrl: link.Url!,
                BodyText: bodyText,
                Deadline: deadline,
                IssuerName: "Ministero della Cultura"));
        }

        return results;
    }
}
