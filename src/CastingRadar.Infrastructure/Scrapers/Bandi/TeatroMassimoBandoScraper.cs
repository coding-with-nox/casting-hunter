using AngleSharp.Dom;
using CastingRadar.Application.Interfaces;
using CastingRadar.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CastingRadar.Infrastructure.Scrapers.Bandi;

public class TeatroMassimoBandoScraper(IHttpClientFactory httpClientFactory, ILogger<TeatroMassimoBandoScraper> logger)
    : BaseBandoScraper(httpClientFactory, logger)
{
    private static readonly string[] PositiveKeywords = ["Audizione", "Concorsi", "Selezione", "Tersicorei", "Orchestra", "Coro", "Maestri collaboratori", "formazioni giovanili"];
    public override string SourceName => "Teatro Massimo";

    protected override async Task<IEnumerable<ScrapedBandoItem>> ScrapeInternalAsync(BandoSource source, CancellationToken ct)
    {
        var doc = await LoadDocumentAsync(source.BaseUrl, ct);
        var candidateLinks = doc.QuerySelectorAll("a[href]")
            .Select(link => new
            {
                Title = CleanText(link.TextContent),
                Url = TryAbsoluteUrl(source.BaseUrl, link.GetAttribute("href"))
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Title) && !string.IsNullOrWhiteSpace(item.Url))
            .Where(item => PositiveKeywords.Any(keyword => item.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            .Where(item => !item.Title.Contains("Leggi di pi", StringComparison.OrdinalIgnoreCase))
            .DistinctBy(item => item.Url, StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();

        var results = new List<ScrapedBandoItem>();
        foreach (var candidate in candidateLinks)
        {
            var bodyText = candidate.Title;
            try
            {
                var detail = await LoadDocumentAsync(candidate.Url!, ct);
                bodyText = CleanText(detail.QuerySelector("main, article, .content, body")?.TextContent);
            }
            catch
            {
                // Keep title fallback.
            }

            results.Add(new ScrapedBandoItem(
                Title: candidate.Title,
                SourceUrl: candidate.Url!,
                BodyText: bodyText,
                Deadline: ExtractItalianDateFromText(bodyText),
                IssuerName: "Fondazione Teatro Massimo"));
        }

        return results;
    }
}
