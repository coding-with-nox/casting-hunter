using CastingRadar.Domain.Enums;

namespace CastingRadar.Domain.ValueObjects;

public record ScraperFilter(
    string[]? Keywords,
    CastingType[]? Types,
    SourceRegion[]? Regions,
    bool OnlyPaid,
    string? GenderFilter,
    int? MinAge,
    int? MaxAge)
{
    public static ScraperFilter Default => new(
        Keywords: null,
        Types: null,
        Regions: [SourceRegion.Italy],
        OnlyPaid: false,
        GenderFilter: "female",
        MinAge: null,
        MaxAge: null);
}
