using CastingRadar.Domain.Entities;

namespace CastingRadar.Application.Interfaces;

public interface IBandoScraperStrategy
{
    string SourceName { get; }
    Task<BandoScrapeSourceResult> ScrapeAsync(BandoSource source, CancellationToken ct = default);
}

public record BandoScrapeSourceResult(IReadOnlyList<ScrapedBandoItem> Items, string? Error = null)
{
    public static BandoScrapeSourceResult Ok(IEnumerable<ScrapedBandoItem> items) =>
        new([.. items]);
    public static BandoScrapeSourceResult Fail(string error) =>
        new([], error);
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
