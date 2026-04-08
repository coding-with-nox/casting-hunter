namespace CastingRadar.Application.DTOs;

public record BandoScrapeResultDto(
    int TotalFound,
    int TotalEligible,
    int TotalNew,
    IReadOnlyList<string> Sources)
{
    public static BandoScrapeResultDto Create(
        int totalFound,
        int totalEligible,
        int totalNew,
        IReadOnlyList<string> sources) =>
        new(totalFound, totalEligible, totalNew, sources);
}
