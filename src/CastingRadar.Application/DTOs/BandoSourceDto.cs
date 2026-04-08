using CastingRadar.Domain.Entities;

namespace CastingRadar.Application.DTOs;

public record BandoSourceDto(
    string Name,
    string Category,
    string BaseUrl,
    int Priority,
    bool IsOfficial,
    bool IsEnabled)
{
    public static BandoSourceDto FromEntity(BandoSource s) => new(
        s.Name,
        s.Category,
        s.BaseUrl,
        s.Priority,
        s.IsOfficial,
        s.IsEnabled);
}
