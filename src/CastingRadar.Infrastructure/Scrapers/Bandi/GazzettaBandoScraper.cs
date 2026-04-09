using AngleSharp.Dom;
using System.Text.RegularExpressions;
using CastingRadar.Application.Interfaces;
using CastingRadar.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CastingRadar.Infrastructure.Scrapers.Bandi;

public class GazzettaBandoScraper(IHttpClientFactory httpClientFactory, ILogger<GazzettaBandoScraper> logger)
    : BaseBandoScraper(httpClientFactory, logger)
{
    public override string SourceName => "Gazzetta Ufficiale - 4a serie concorsi";

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
            .Where(item => item.Url!.Contains("/atto/", StringComparison.OrdinalIgnoreCase)
                || item.Title.Contains("concorso", StringComparison.OrdinalIgnoreCase)
                || item.Title.Contains("selezione", StringComparison.OrdinalIgnoreCase)
                || item.Title.Contains("teatro", StringComparison.OrdinalIgnoreCase)
                || item.Title.Contains("coro", StringComparison.OrdinalIgnoreCase)
                || item.Title.Contains("orchestra", StringComparison.OrdinalIgnoreCase))
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
                bodyText = CleanText(detail.QuerySelector("main, article, .atto, .testo, body")?.TextContent);
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
                PublishedAt: ExtractIssueDate(link.Url!),
                Deadline: deadline,
                IssuerName: "Gazzetta Ufficiale"));
        }

        return results;
    }

    private static DateTime? ExtractIssueDate(string url)
    {
        var match = Regex.Match(url, @"/(\d{4})/(\d{2})/(\d{2})/");
        if (!match.Success)
        {
            return null;
        }

        return DateTime.TryParse($"{match.Groups[1].Value}-{match.Groups[2].Value}-{match.Groups[3].Value}", out var parsed)
            ? parsed
            : null;
    }
}
