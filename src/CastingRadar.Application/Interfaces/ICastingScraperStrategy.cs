using CastingRadar.Domain.Entities;
using CastingRadar.Domain.Enums;
using CastingRadar.Domain.ValueObjects;

namespace CastingRadar.Application.Interfaces;

public interface ICastingScraperStrategy
{
    string SourceName { get; }
    SourceRegion Region { get; }
    bool IsEnabled { get; }

    /// <summary>
    /// Scrapes the source and returns found casting calls.
    /// Must not throw — returns empty list on any error.
    /// </summary>
    Task<IEnumerable<CastingCall>> ScrapeAsync(
        ScraperFilter filter,
        CancellationToken cancellationToken = default);
}
