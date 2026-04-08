using AngleSharp.Dom;
using CastingRadar.Application.Interfaces;
using CastingRadar.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CastingRadar.Infrastructure.Scrapers.Bandi;

public class TeatroStabileTorinoBandoScraper(IHttpClientFactory httpClientFactory, ILogger<TeatroStabileTorinoBandoScraper> logger)
    : BaseBandoScraper(httpClientFactory, logger)
{
    public override string SourceName => "Teatro Stabile di Torino";

    protected override async Task<IEnumerable<ScrapedBandoItem>> ScrapeInternalAsync(BandoSource source, CancellationToken ct)
    {
        var doc = await LoadDocumentAsync("https://www.teatrostabiletorino.it/scuola-per-attori/", ct);
        var links = doc.QuerySelectorAll("a[href]")
            .Select(link => new
            {
                Title = CleanText(link.TextContent),
                Url = TryAbsoluteUrl(source.BaseUrl, link.GetAttribute("href"))
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Url))
            .Where(item => item.Title.StartsWith("Bando Triennio", StringComparison.OrdinalIgnoreCase))
            .Take(3)
            .ToList();

        return links.Select(item => new ScrapedBandoItem(
            Title: $"Bando corso per attori - {item.Title}",
            SourceUrl: item.Url!,
            BodyText: $"Scuola per attori del Teatro Stabile di Torino. {item.Title}.",
            IssuerName: "Teatro Stabile di Torino"))
            .ToList();
    }
}
