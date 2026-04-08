using AngleSharp;
using AngleSharp.Dom;
using CastingRadar.Application.Interfaces;
using CastingRadar.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CastingRadar.Infrastructure.Scrapers.Bandi;

public abstract class BaseBandoScraper(IHttpClientFactory httpClientFactory, ILogger logger) : IBandoScraperStrategy
{
    public abstract string SourceName { get; }
    protected virtual string HttpClientName => "Scraper";

    public async Task<IEnumerable<ScrapedBandoItem>> ScrapeAsync(BandoSource source, CancellationToken ct = default)
    {
        try
        {
            return await ScrapeInternalAsync(source, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Bando scraper {Source} encountered an error", SourceName);
            return [];
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
        {
            return null;
        }

        if (Uri.TryCreate(rawUrl, UriKind.Absolute, out var absolute))
        {
            return absolute.ToString();
        }

        if (baseUrl is not null && Uri.TryCreate(new Uri(baseUrl), rawUrl, out var relative))
        {
            return relative.ToString();
        }

        return null;
    }

    protected abstract Task<IEnumerable<ScrapedBandoItem>> ScrapeInternalAsync(BandoSource source, CancellationToken ct);
}
