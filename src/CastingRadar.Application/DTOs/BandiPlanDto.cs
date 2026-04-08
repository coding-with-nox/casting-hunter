namespace CastingRadar.Application.DTOs;

public record BandiPlanDto(
    string Status,
    string Summary,
    IReadOnlyList<BandiPriorityStepDto> PriorityPlan,
    IReadOnlyList<string> ExtractionFlow,
    IReadOnlyList<string> IssuerTypes,
    IReadOnlyList<BandiSourceGroupDto> SourceGroups);

public record BandiPriorityStepDto(
    string Title,
    string Detail);

public record BandiSourceGroupDto(
    string Name,
    IReadOnlyList<string> Items);
