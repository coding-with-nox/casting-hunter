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
    DateTime CreatedAt,
    string? UserStatus,
    IReadOnlyList<string> ReviewSignals)
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
        b.CreatedAt,
        b.UserStatus,
        BuildReviewSignals(b));

    private static IReadOnlyList<string> BuildReviewSignals(Bando b)
    {
        var signals = new List<string>();

        if (b.Status == "Da rivedere")
        {
            signals.Add("Confidenza bassa o classificazione incompleta");
        }

        if (b.ConfidenceScore < 0.78m)
        {
            signals.Add($"Confidenza {b.ConfidenceScore:0.00}");
        }

        if (string.IsNullOrWhiteSpace(b.Role))
        {
            signals.Add("Ruolo non rilevato");
        }

        if (string.IsNullOrWhiteSpace(b.Discipline) || b.Discipline == "Spettacolo")
        {
            signals.Add("Disciplina generica");
        }

        if (!b.Deadline.HasValue)
        {
            signals.Add("Scadenza non indicata");
        }

        if (string.IsNullOrWhiteSpace(b.Location))
        {
            signals.Add("Luogo non indicato");
        }

        if (b.BodyText.Length < 120)
        {
            signals.Add("Descrizione breve");
        }

        return signals;
    }
}
