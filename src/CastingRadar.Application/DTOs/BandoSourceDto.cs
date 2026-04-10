using CastingRadar.Domain.Entities;

namespace CastingRadar.Application.DTOs;

public record BandoSourceDto(
    int Id,
    string Name,
    string Category,
    string BaseUrl,
    int Priority,
    bool IsOfficial,
    bool IsEnabled,
    string? Regione,
    DateTime? LastRunAt,
    int LastRunFound,
    int LastRunEligible,
    int LastRunNew,
    string? LastRunError)
{
    public static BandoSourceDto FromEntity(BandoSource s) => new(
        s.Id,
        s.Name,
        s.Category,
        s.BaseUrl,
        s.Priority,
        s.IsOfficial,
        s.IsEnabled,
        s.Regione,
        s.LastRunAt,
        s.LastRunFound,
        s.LastRunEligible,
        s.LastRunNew,
        s.LastRunError);
}
