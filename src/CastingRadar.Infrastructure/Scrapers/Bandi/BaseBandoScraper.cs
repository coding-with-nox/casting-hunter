using AngleSharp;
using AngleSharp.Dom;
using CastingRadar.Application.Interfaces;
using CastingRadar.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CastingRadar.Infrastructure.Scrapers.Bandi;

public abstract class BaseBandoScraper(IHttpClientFactory httpClientFactory, ILogger logger) : IBandoScraperStrategy
{
    public abstract string SourceName { get; }
    protected virtual string HttpClientName => "Scraper";

    public async Task<BandoScrapeSourceResult> ScrapeAsync(BandoSource source, CancellationToken ct = default)
    {
        try
        {
            var items = (await ScrapeInternalAsync(source, ct)).ToList();
            if (items.Count == 0)
                logger.LogWarning("Bando scraper {Source} returned no items", SourceName);
            return BandoScrapeSourceResult.Ok(items);
        }
        catch (HttpRequestException ex)
        {
            var msg = ex.StatusCode.HasValue
                ? $"HTTP {(int)ex.StatusCode}"
                : $"Endpoint non raggiungibile";
            logger.LogWarning("Bando scraper {Source} HTTP error: {Msg}", SourceName, msg);
            return BandoScrapeSourceResult.Fail(msg);
        }
        catch (TaskCanceledException)
        {
            logger.LogWarning("Bando scraper {Source} timed out", SourceName);
            return BandoScrapeSourceResult.Fail("Timeout");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Bando scraper {Source} encountered an error", SourceName);
            return BandoScrapeSourceResult.Fail(ex.Message.Length > 200 ? ex.Message[..200] : ex.Message);
        }
    }

    protected HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient(HttpClientName);
        client.DefaultRequestHeaders.Remove("User-Agent");
        client.DefaultRequestHeaders.Add("User-Agent", Http.ScraperHttpClientFactory.NextUserAgent());
        return client;
    }

    protected async Task<IDocument> LoadDocumentAsync(string url, CancellationToken ct)
    {
        using var client = CreateClient();
        var html = await client.GetStringAsync(url, ct);
        var context = BrowsingContext.New(Configuration.Default);
        return await context.OpenAsync(req => req.Content(html), ct);
    }

    protected static string CleanText(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : System.Text.RegularExpressions.Regex.Replace(value, @"\s+", " ").Trim();

    protected static string? TryAbsoluteUrl(string? baseUrl, string? rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
            return null;

        // Only accept http/https — avoids file:// URIs che .NET genera da path assoluti tipo /news/...
        if (Uri.TryCreate(rawUrl, UriKind.Absolute, out var absolute)
            && (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
        {
            return absolute.ToString();
        }

        if (baseUrl is not null && Uri.TryCreate(new Uri(baseUrl), rawUrl, out var relative)
            && (relative.Scheme == Uri.UriSchemeHttp || relative.Scheme == Uri.UriSchemeHttps))
        {
            return relative.ToString();
        }

        return null;
    }

    protected static DateTime? ExtractItalianDateFromText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        // Patterns preceded by deadline keywords (must be checked before bare dates)
        // Group 1: dd/mm/yyyy  or  dd.mm.yyyy  or  dd-mm-yyyy
        // Group 2: dd MMMM yyyy  (Italian month name)
        var deadlinePatterns = new[]
        {
            // "entro il", "non oltre il", "entro e non oltre", "scadenza:", "termine:", "chiusura:", "presentazione domande entro"
            @"(?:entro(?:\s+e\s+non\s+oltre)?(?:\s+il)?|non\s+oltre(?:\s+il)?|scadenza[:\s]+|termine[:\s]+|chiusura[:\s]+|presentazione.*?entro(?:\s+il)?|domande.*?entro(?:\s+il)?|fa\s+pervenire.*?entro(?:\s+il)?)\s*(?<date>\d{1,2}[/.\-]\d{1,2}[/.\-]\d{2,4}|\d{1,2}\s+[A-Za-zÀ-ÿ]+\s+\d{4})",
            // "scade il", "scade entro"
            @"scade(?:\s+entro)?(?:\s+il)?\s+(?<date>\d{1,2}[/.\-]\d{1,2}[/.\-]\d{2,4}|\d{1,2}\s+[A-Za-zÀ-ÿ]+\s+\d{4})",
            // bare "dd MMMM yyyy" (fallback, lower priority)
            @"(?<date>\d{1,2}\s+(?:gennaio|febbraio|marzo|aprile|maggio|giugno|luglio|agosto|settembre|ottobre|novembre|dicembre)\s+\d{4})",
        };

        var itCulture = CultureInfo.GetCultureInfo("it-IT");

        foreach (var pattern in deadlinePatterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (!match.Success)
                continue;

            var raw = match.Groups["date"].Value.Trim();

            // Normalize slashes/dots to dashes so TryParse handles dd-MM-yyyy
            var normalized = Regex.Replace(raw, @"[/.]", "-");

            if (DateTime.TryParse(normalized, itCulture, DateTimeStyles.AssumeLocal, out var parsed)
                && parsed.Year >= 2020 && parsed.Year <= 2035)
            {
                return parsed;
            }

            // Try explicit dd-MM-yyyy format for numeric dates
            if (DateTime.TryParseExact(normalized,
                ["d-M-yyyy", "d-M-yy", "dd-MM-yyyy", "dd-MM-yy"],
                itCulture, DateTimeStyles.AssumeLocal, out var parsed2)
                && parsed2.Year >= 2020 && parsed2.Year <= 2035)
            {
                return parsed2;
            }
        }

        return null;
    }

    protected abstract Task<IEnumerable<ScrapedBandoItem>> ScrapeInternalAsync(BandoSource source, CancellationToken ct);
}
