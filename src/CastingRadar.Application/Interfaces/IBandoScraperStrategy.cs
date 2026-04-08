using CastingRadar.Domain.Entities;

namespace CastingRadar.Application.Interfaces;

public interface IBandoScraperStrategy
{
    string SourceName { get; }
    Task<IEnumerable<ScrapedBandoItem>> ScrapeAsync(BandoSource source, CancellationToken ct = default);
}

public record ScrapedBandoItem(
    string Title,
    string SourceUrl,
    string BodyText,
    string? ApplicationUrl = null,
    DateTime? PublishedAt = null,
    DateTime? Deadline = null,
    string? Location = null,
    string? IssuerName = null);
