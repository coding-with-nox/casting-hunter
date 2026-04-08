using CastingRadar.Domain.Entities;
using CastingRadar.Domain.Enums;

namespace CastingRadar.Application.DTOs;

public record SourceStatusDto(
    string Name,
    SourceRegion Region,
    string? Url,
    bool IsEnabled,
    DateTime? LastScrapedAt,
    int ErrorCount,
    bool HasCustomScraper)
{
    public static SourceStatusDto FromEntity(Source s, bool hasCustomScraper = false) =>
        new(s.Name, s.Region, s.Url, s.IsEnabled, s.LastScrapedAt, s.ErrorCount, hasCustomScraper);

    public static SourceStatusDto FromScraper(string name, SourceRegion region) =>
        new(name, region, null, true, null, 0, true);
}
