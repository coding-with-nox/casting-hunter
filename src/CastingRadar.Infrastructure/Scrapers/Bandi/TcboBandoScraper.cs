using AngleSharp.Dom;
using CastingRadar.Application.Interfaces;
using CastingRadar.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CastingRadar.Infrastructure.Scrapers.Bandi;

public class TcboBandoScraper(IHttpClientFactory httpClientFactory, ILogger<TcboBandoScraper> logger)
    : BaseBandoScraper(httpClientFactory, logger)
{
    public override string SourceName => "Teatro Comunale di Bologna";

    protected override async Task<IEnumerable<ScrapedBandoItem>> ScrapeInternalAsync(BandoSource source, CancellationToken ct)
    {
        var doc = await LoadDocumentAsync(source.BaseUrl, ct);

        return doc.QuerySelectorAll("a[href]")
            .Select(link => new
            {
                Title = CleanText(link.TextContent),
                Url = TryAbsoluteUrl(source.BaseUrl, link.GetAttribute("href"))
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Title) && !string.IsNullOrWhiteSpace(item.Url))
            .Where(item =>
                item.Title.Contains("Audizione", StringComparison.OrdinalIgnoreCase) ||
                item.Title.Contains("Selezione", StringComparison.OrdinalIgnoreCase) ||
                item.Title.Contains("Artisti del coro", StringComparison.OrdinalIgnoreCase) ||
                item.Title.Contains("Maestro collaboratore", StringComparison.OrdinalIgnoreCase))
            .Where(item => !item.Title.Contains("ESITO", StringComparison.OrdinalIgnoreCase))
            .Where(item => !item.Title.Contains("Programma d'esame", StringComparison.OrdinalIgnoreCase))
            .DistinctBy(item => item.Url, StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .Select(item => new ScrapedBandoItem(
                Title: item.Title,
                SourceUrl: item.Url!,
                BodyText: item.Title,
                IssuerName: "Fondazione Teatro Comunale di Bologna"))
            .ToList();
    }
}
