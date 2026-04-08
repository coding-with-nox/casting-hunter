using AngleSharp.Dom;
using CastingRadar.Application.Interfaces;
using CastingRadar.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CastingRadar.Infrastructure.Scrapers.Bandi;

public class OperaRomaBandoScraper(IHttpClientFactory httpClientFactory, ILogger<OperaRomaBandoScraper> logger)
    : BaseBandoScraper(httpClientFactory, logger)
{
    private const string ListUrl = "https://www.operaroma.it/bandi-e-concorsi";

    public override string SourceName => "Teatro dell Opera di Roma";

    protected override async Task<IEnumerable<ScrapedBandoItem>> ScrapeInternalAsync(BandoSource source, CancellationToken ct)
    {
        var doc = await LoadDocumentAsync(ListUrl, ct);
        var openHeading = doc.QuerySelectorAll("h1")
            .FirstOrDefault(element => CleanText(element.TextContent).Contains("Bandi aperti", StringComparison.OrdinalIgnoreCase));

        if (openHeading is null)
        {
            return [];
        }

        var results = new List<ScrapedBandoItem>();
        var current = openHeading.NextElementSibling;

        while (current is not null)
        {
            if (string.Equals(current.LocalName, "h1", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (string.Equals(current.LocalName, "h4", StringComparison.OrdinalIgnoreCase))
            {
                var anchor = current.QuerySelector("a[href]");
                var title = anchor is null ? CleanText(current.TextContent) : CleanText(anchor.TextContent);
                var sourceUrl = TryAbsoluteUrl(ListUrl, anchor?.GetAttribute("href")) ?? ListUrl;
                var bodyParts = new List<string>();
                var scan = current.NextElementSibling;

                while (scan is not null &&
                       !string.Equals(scan.LocalName, "h4", StringComparison.OrdinalIgnoreCase) &&
                       !string.Equals(scan.LocalName, "h1", StringComparison.OrdinalIgnoreCase))
                {
                    var text = CleanText(scan.TextContent);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        bodyParts.Add(text);
                    }

                    scan = scan.NextElementSibling;
                }

                var bodyText = string.Join(" ", bodyParts);
                results.Add(new ScrapedBandoItem(
                    Title: title,
                    SourceUrl: sourceUrl,
                    BodyText: bodyText,
                    Deadline: ExtractItalianDateFromText(bodyText),
                    IssuerName: "Fondazione Teatro dell'Opera di Roma"));

                current = scan;
                continue;
            }

            current = current.NextElementSibling;
        }

        return results;
    }
}
