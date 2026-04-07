using CastingRadar.Domain.Entities;
using CastingRadar.Domain.Enums;

namespace CastingRadar.Application.DTOs;

public record SourceStatusDto(
    string Name,
    SourceRegion Region,
    bool IsEnabled,
    DateTime? LastScrapedAt,
    int ErrorCount)
{
    public static SourceStatusDto FromEntity(Source s) =>
        new(s.Name, s.Region, s.IsEnabled, s.LastScrapedAt, s.ErrorCount);
}
