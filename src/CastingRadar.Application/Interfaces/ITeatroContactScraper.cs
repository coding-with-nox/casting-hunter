using CastingRadar.Domain.Entities;

namespace CastingRadar.Application.Interfaces;

public interface ITeatroContactScraper
{
    Task<TeatroContactScrapeResult> ScrapeAsync(BandoSource source, CancellationToken ct = default);
    Task<TeatroContactScrapeResult> ScrapeByUrlAsync(string name, string url, CancellationToken ct = default);
}

public record TeatroContactScrapeResult(
    string? Email,
    string? Phone,
    string? Address,
    string? ContactPageUrl,
    string? Notes,
    string? Error = null);
