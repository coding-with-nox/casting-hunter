using AngleSharp;
using AngleSharp.Dom;
using CastingRadar.Application.Interfaces;
using CastingRadar.Domain.Entities;
using CastingRadar.Domain.Enums;
using CastingRadar.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace CastingRadar.Infrastructure.Scrapers;

public abstract class BaseScraper(IHttpClientFactory httpClientFactory, ILogger logger) : ICastingScraperStrategy
{
    public abstract string SourceName { get; }
    public abstract SourceRegion Region { get; }
    public virtual bool IsEnabled { get; set; } = true;

    protected abstract string HttpClientName { get; }

    protected HttpClient CreateClient()
    {
        var client = httpClientFactory.CreateClient(HttpClientName);
        // Rotate user-agent per request
        client.DefaultRequestHeaders.Remove("User-Agent");
        client.DefaultRequestHeaders.Add("User-Agent", Http.ScraperHttpClientFactory.NextUserAgent());
        return client;
    }

    protected async Task<IDocument> LoadDocumentAsync(string url, CancellationToken ct)
    {
        var client = CreateClient();
        var html = await client.GetStringAsync(url, ct);
        var config = Configuration.Default;
        var context = BrowsingContext.New(config);
        return await context.OpenAsync(req => req.Content(html), ct);
    }

    public async Task<IEnumerable<CastingCall>> ScrapeAsync(ScraperFilter filter, CancellationToken cancellationToken = default)
    {
        try
        {
            return await ScrapeInternalAsync(filter, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Scraper {Source} encountered an error", SourceName);
            return [];
        }
    }

    protected abstract Task<IEnumerable<CastingCall>> ScrapeInternalAsync(ScraperFilter filter, CancellationToken ct);

    protected static string? ParseText(IElement? el) => el?.TextContent.Trim().NullIfEmpty();
    protected static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}

internal static class StringExtensions
{
    public static string? NullIfEmpty(this string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
