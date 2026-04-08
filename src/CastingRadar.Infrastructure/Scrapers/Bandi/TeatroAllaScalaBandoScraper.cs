using AngleSharp.Dom;
using CastingRadar.Application.Interfaces;
using CastingRadar.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CastingRadar.Infrastructure.Scrapers.Bandi;

public class TeatroAllaScalaBandoScraper(IHttpClientFactory httpClientFactory, ILogger<TeatroAllaScalaBandoScraper> logger)
    : BaseBandoScraper(httpClientFactory, logger)
{
    private static readonly string[] PageUrls =
    [
        "https://www.teatroallascala.org/it/il-teatro/lavora-con-noi/concorsi-e-audizioni-orchestra.html",
        "https://www.teatroallascala.org/it/il-teatro/lavora-con-noi/concorsi-e-audizioni-corpo-di-ballo.html",
        "https://www.teatroallascala.org/it/il-teatro/lavora-con-noi/concorsi-e-audizioni-coro.html"
    ];

    public override string SourceName => "Teatro alla Scala";

    protected override async Task<IEnumerable<ScrapedBandoItem>> ScrapeInternalAsync(BandoSource source, CancellationToken ct)
    {
        var results = new List<ScrapedBandoItem>();

        foreach (var pageUrl in PageUrls)
        {
            results.AddRange(await ScrapeOpenBandiPageAsync(pageUrl, ct));
        }

        return results
            .DistinctBy(item => item.SourceUrl, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<IEnumerable<ScrapedBandoItem>> ScrapeOpenBandiPageAsync(string url, CancellationToken ct)
    {
        var doc = await LoadDocumentAsync(url, ct);
        var openHeading = doc.QuerySelectorAll("h2")
            .FirstOrDefault(element => CleanText(element.TextContent).Contains("Bandi aperti", StringComparison.OrdinalIgnoreCase));

        if (openHeading is null)
        {
            return [];
        }

        var results = new List<ScrapedBandoItem>();
        var current = openHeading.NextElementSibling;
        string? currentTitle = null;
        string? bandoUrl = null;
        string? applicationUrl = null;
        var bodyParts = new List<string>();

        void Flush()
        {
            if (string.IsNullOrWhiteSpace(currentTitle) || string.IsNullOrWhiteSpace(bandoUrl))
            {
                return;
            }

            var bodyText = string.Join(" ", bodyParts.Where(part => !string.IsNullOrWhiteSpace(part)));
            results.Add(new ScrapedBandoItem(
                Title: currentTitle,
                SourceUrl: bandoUrl,
                BodyText: CleanText(bodyText),
                ApplicationUrl: applicationUrl,
                Deadline: ExtractItalianDateFromText($"{currentTitle} {bodyText}"),
                IssuerName: "Fondazione Teatro alla Scala"));
        }

        while (current is not null)
        {
            if (string.Equals(current.LocalName, "h2", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            if (string.Equals(current.LocalName, "h3", StringComparison.OrdinalIgnoreCase))
            {
                Flush();
                currentTitle = CleanText(current.TextContent);
                bandoUrl = null;
                applicationUrl = null;
                bodyParts.Clear();
                current = current.NextElementSibling;
                continue;
            }

            var text = CleanText(current.TextContent);
            if (!string.IsNullOrWhiteSpace(text))
            {
                bodyParts.Add(text);
            }

            IEnumerable<IElement> links = string.Equals(current.LocalName, "a", StringComparison.OrdinalIgnoreCase)
                ? new[] { current }
                : current.QuerySelectorAll("a[href]");

            foreach (var link in links)
            {
                var linkText = CleanText(link.TextContent);
                var href = TryAbsoluteUrl(url, link.GetAttribute("href"));
                if (string.IsNullOrWhiteSpace(href))
                {
                    continue;
                }

                if (linkText.Contains("scaricare il bando", StringComparison.OrdinalIgnoreCase))
                {
                    bandoUrl = href;
                }
                else if (linkText.Contains("compilare la domanda", StringComparison.OrdinalIgnoreCase))
                {
                    applicationUrl = href;
                }
            }

            current = current.NextElementSibling;
        }

        Flush();
        return results;
    }
}
