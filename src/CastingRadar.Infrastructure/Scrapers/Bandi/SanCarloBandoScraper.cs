using AngleSharp.Dom;
using CastingRadar.Application.Interfaces;
using CastingRadar.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CastingRadar.Infrastructure.Scrapers.Bandi;

public class SanCarloBandoScraper(IHttpClientFactory httpClientFactory, ILogger<SanCarloBandoScraper> logger)
    : BaseBandoScraper(httpClientFactory, logger)
{
    private const string DetailUrl = "https://www.teatrosancarlo.it/news/selezione-ammissione-corsi-propedeutici-scuola-ballo-2026-27/";

    public override string SourceName => "Teatro di San Carlo";

    protected override async Task<IEnumerable<ScrapedBandoItem>> ScrapeInternalAsync(BandoSource source, CancellationToken ct)
    {
        var doc = await LoadDocumentAsync(DetailUrl, ct);
        var title = CleanText(doc.QuerySelector("h1")?.TextContent);
        var bodyText = CleanText(doc.QuerySelector("main, article, .content, body")?.TextContent);
        var bandoUrl = doc.QuerySelectorAll("a[href]")
            .FirstOrDefault(link => CleanText(link.TextContent).Contains("Bando", StringComparison.OrdinalIgnoreCase))
            ?.GetAttribute("href");

        var applicationUrl = doc.QuerySelectorAll("a[href]")
            .FirstOrDefault(link => CleanText(link.TextContent).Contains("Domanda", StringComparison.OrdinalIgnoreCase))
            ?.GetAttribute("href");

        if (string.IsNullOrWhiteSpace(title))
        {
            return [];
        }

        return
        [
            new ScrapedBandoItem(
                Title: title,
                SourceUrl: TryAbsoluteUrl(DetailUrl, bandoUrl) ?? DetailUrl,
                BodyText: bodyText,
                ApplicationUrl: TryAbsoluteUrl(DetailUrl, applicationUrl),
                PublishedAt: ExtractItalianDateFromText(bodyText),
                Deadline: ExtractItalianDateFromText(bodyText),
                IssuerName: "Fondazione Teatro di San Carlo")
        ];
    }
}
