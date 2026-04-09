using AngleSharp;
using AngleSharp.Dom;
using CastingRadar.Application.Interfaces;
using CastingRadar.Domain.Entities;
using CastingRadar.Infrastructure.Http;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace CastingRadar.Infrastructure.Scrapers;

public class TeatroContactScraper(IHttpClientFactory httpClientFactory, ILogger<TeatroContactScraper> logger)
    : ITeatroContactScraper
{
    // Paths to try in order — first match wins
    private static readonly string[] ContactPaths =
    [
        "/contatti", "/contact", "/contacts",
        "/chi-siamo/contatti", "/chi-siamo",
        "/il-teatro/contatti", "/il-teatro",
        "/about/contact", "/about",
        "/dove-siamo", "/info",
        "/uffici", "/segreteria",
    ];

    private static readonly Regex EmailRx = new(
        @"\b[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PhoneRx = new(
        @"(?:\+39[\s\-]?)?(?:0[1-9]\d{1,4}[\s\-]?\d{3,8}|3\d{2}[\s\-]?\d{6,7})",
        RegexOptions.Compiled);

    private static readonly Regex AddressRx = new(
        @"(?:Via|Viale|Piazza|Corso|Largo|Vicolo|Strada|Lungarno)\s+[A-Za-zÀ-ÿ\s'\.,\d]{5,60}(?:,\s*\d{5})?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Emails that are not useful (privacy@, noreply@, etc.)
    private static readonly string[] IgnoredEmailPrefixes =
    [
        "privacy@", "noreply@", "no-reply@", "webmaster@", "postmaster@", "info@example"
    ];

    public Task<TeatroContactScrapeResult> ScrapeAsync(BandoSource source, CancellationToken ct = default) =>
        ScrapeInternalAsync(source.Name, source.BaseUrl, ct);

    public Task<TeatroContactScrapeResult> ScrapeByUrlAsync(string name, string url, CancellationToken ct = default) =>
        ScrapeInternalAsync(name, url, ct);

    private async Task<TeatroContactScrapeResult> ScrapeInternalAsync(string name, string baseUrl, CancellationToken ct)
    {
        try
        {
            var baseUri = new Uri(baseUrl.TrimEnd('/'));
            string? email = null, phone = null, address = null, contactPageUrl = null;

            // 1. Try dedicated contact paths first
            foreach (var path in ContactPaths)
            {
                if (ct.IsCancellationRequested) break;

                var url = $"{baseUri.Scheme}://{baseUri.Host}{path}";
                try
                {
                    var (doc, finalUrl) = await LoadAsync(url, ct);
                    var text = doc.Body?.TextContent ?? string.Empty;
                    var html = doc.DocumentElement?.OuterHtml ?? string.Empty;

                    email ??= ExtractBestEmail(html + " " + text, baseUrl);
                    phone ??= ExtractPhone(text);
                    address ??= ExtractAddress(text);

                    if (email is not null || phone is not null || address is not null)
                    {
                        contactPageUrl = finalUrl ?? url;
                        break;
                    }
                }
                catch { /* try next */ }
            }

            // 2. Fallback: scrape the base URL itself
            if (email is null && phone is null)
            {
                try
                {
                    var (doc, _) = await LoadAsync(baseUrl, ct);
                    var text = doc.Body?.TextContent ?? string.Empty;
                    var html = doc.DocumentElement?.OuterHtml ?? string.Empty;
                    email ??= ExtractBestEmail(html + " " + text, baseUrl);
                    phone ??= ExtractPhone(text);
                    address ??= ExtractAddress(text);
                    contactPageUrl ??= baseUrl;
                }
                catch { /* ignore */ }
            }

            var notes = (email is null && phone is null && address is null)
                ? "Nessun dato di contatto trovato automaticamente"
                : null;

            return new TeatroContactScrapeResult(email, phone, address, contactPageUrl, notes);
        }
        catch (HttpRequestException ex)
        {
            var msg = ex.StatusCode.HasValue ? $"HTTP {(int)ex.StatusCode}" : "Endpoint non raggiungibile";
            logger.LogWarning("TeatroContactScraper {Name}: {Msg}", name, msg);
            return new TeatroContactScrapeResult(null, null, null, null, null, msg);
        }
        catch (TaskCanceledException)
        {
            return new TeatroContactScrapeResult(null, null, null, null, null, "Timeout");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TeatroContactScraper {Name} error", name);
            return new TeatroContactScrapeResult(null, null, null, null, null, ex.Message[..Math.Min(200, ex.Message.Length)]);
        }
    }

    private async Task<(IDocument doc, string? finalUrl)> LoadAsync(string url, CancellationToken ct)
    {
        using var client = httpClientFactory.CreateClient("Scraper");
        client.DefaultRequestHeaders.Remove("User-Agent");
        client.DefaultRequestHeaders.Add("User-Agent", ScraperHttpClientFactory.NextUserAgent());

        var response = await client.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync(ct);
        var finalUrl = response.RequestMessage?.RequestUri?.ToString();

        var context = BrowsingContext.New(Configuration.Default);
        var doc = await context.OpenAsync(req => req.Content(html), ct);
        return (doc, finalUrl);
    }

    private static string? ExtractBestEmail(string text, string baseUrl)
    {
        var domain = TryGetDomain(baseUrl);
        var allEmails = EmailRx.Matches(text)
            .Select(m => m.Value.ToLowerInvariant())
            .Where(e => !IgnoredEmailPrefixes.Any(p => e.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            .Distinct()
            .ToList();

        if (allEmails.Count == 0) return null;

        // Prefer emails from the same domain as the website
        if (domain is not null)
        {
            var domainEmail = allEmails.FirstOrDefault(e => e.EndsWith($"@{domain}") || e.Contains($".{domain}"));
            if (domainEmail is not null) return domainEmail;
        }

        // Then prefer emails with artistic / contact prefixes
        var preferred = allEmails.FirstOrDefault(e =>
            e.StartsWith("info@") || e.StartsWith("biglietteria@") ||
            e.StartsWith("segreteria@") || e.StartsWith("contatti@") ||
            e.StartsWith("direzione@") || e.StartsWith("ufficiostampa@"));

        return preferred ?? allEmails[0];
    }

    private static string? ExtractPhone(string text)
    {
        var match = PhoneRx.Match(text);
        if (!match.Success) return null;
        // Normalize whitespace in phone
        return Regex.Replace(match.Value.Trim(), @"\s+", " ");
    }

    private static string? ExtractAddress(string text)
    {
        var match = AddressRx.Match(text);
        if (!match.Success) return null;
        return Regex.Replace(match.Value.Trim(), @"\s+", " ");
    }

    private static string? TryGetDomain(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;
        var host = uri.Host.ToLowerInvariant();
        // Strip www.
        return host.StartsWith("www.") ? host[4..] : host;
    }
}
