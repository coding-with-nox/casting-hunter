using CastingRadar.Domain.Entities;

namespace CastingRadar.Application.DTOs;

public record BandoDto(
    Guid Id,
    string Title,
    string IssuerName,
    string IssuerType,
    string SourceName,
    string SourceUrl,
    string? ApplicationUrl,
    DateTime? PublishedAt,
    DateTime? Deadline,
    string? Location,
    string? Discipline,
    string? Role,
    string BodyText,
    bool IsPublic,
    decimal ConfidenceScore,
    string Status,
    DateTime CreatedAt)
{
    public static BandoDto FromEntity(Bando b) => new(
        b.Id,
        b.Title,
        b.IssuerName,
        b.IssuerType,
        b.SourceName,
        b.SourceUrl,
        b.ApplicationUrl,
        b.PublishedAt,
        b.Deadline,
        b.Location,
        b.Discipline,
        b.Role,
        b.BodyText,
        b.IsPublic,
        b.ConfidenceScore,
        b.Status,
        b.CreatedAt);
}
