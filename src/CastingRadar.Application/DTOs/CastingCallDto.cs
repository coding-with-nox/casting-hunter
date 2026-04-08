using CastingRadar.Domain.Entities;
using CastingRadar.Domain.Enums;

namespace CastingRadar.Application.DTOs;

public record CastingCallDto(
    Guid Id,
    string Title,
    string Description,
    string SourceUrl,
    string SourceName,
    CastingType Type,
    SourceRegion Region,
    string? Location,
    DateTime? Deadline,
    bool IsPaid,
    string? RequiredGender,
    string? AgeRange,
    string? Requirements,
    bool IsFavorite,
    bool IsApplied,
    bool IsHidden,
    DateTime ScrapedAt)
{
    public static CastingCallDto FromEntity(CastingCall c) => new(
        c.Id, c.Title, c.Description, c.SourceUrl, c.SourceName,
        c.Type, c.Region, c.Location, c.Deadline, c.IsPaid,
        c.RequiredGender, c.AgeRange, c.Requirements,
        c.IsFavorite, c.IsApplied, c.IsHidden, c.ScrapedAt);
}
