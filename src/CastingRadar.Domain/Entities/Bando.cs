using System.Security.Cryptography;
using System.Text;

namespace CastingRadar.Domain.Entities;

public class Bando
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string IssuerName { get; private set; } = string.Empty;
    public string IssuerType { get; private set; } = string.Empty;
    public string SourceName { get; private set; } = string.Empty;
    public string SourceUrl { get; private set; } = string.Empty;
    public string? ApplicationUrl { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public DateTime? Deadline { get; private set; }
    public string? Location { get; private set; }
    public string? Discipline { get; private set; }
    public string? Role { get; private set; }
    public string BodyText { get; private set; } = string.Empty;
    public bool IsPublic { get; private set; }
    public decimal ConfidenceScore { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public string ContentHash { get; private set; } = string.Empty;
    /// <summary>null = non valutato, "Considerato" = interesse, "Escluso" = scartato</summary>
    public string? UserStatus { get; private set; }

    private Bando() { }

    public static Bando Create(
        string title,
        string issuerName,
        string issuerType,
        string sourceName,
        string sourceUrl,
        string bodyText,
        bool isPublic,
        decimal confidenceScore,
        string status,
        string? applicationUrl = null,
        DateTime? publishedAt = null,
        DateTime? deadline = null,
        string? location = null,
        string? discipline = null,
        string? role = null)
    {
        var hash = ComputeHash(title, issuerName, sourceUrl);
        return new Bando
        {
            Id = Guid.NewGuid(),
            Title = title,
            IssuerName = issuerName,
            IssuerType = issuerType,
            SourceName = sourceName,
            SourceUrl = sourceUrl,
            ApplicationUrl = applicationUrl,
            PublishedAt = publishedAt,
            Deadline = deadline,
            Location = location,
            Discipline = discipline,
            Role = role,
            BodyText = bodyText,
            IsPublic = isPublic,
            ConfidenceScore = confidenceScore,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            ContentHash = hash
        };
    }

    public void SetUserStatus(string? status) => UserStatus = status;

    public static string ComputeHash(string title, string issuerName, string sourceUrl)
    {
        var input = $"{title.Trim().ToLowerInvariant()}|{issuerName.Trim().ToLowerInvariant()}|{sourceUrl.Trim().ToLowerInvariant()}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
