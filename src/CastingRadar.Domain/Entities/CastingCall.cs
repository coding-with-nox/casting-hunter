using System.Security.Cryptography;
using System.Text;
using CastingRadar.Domain.Enums;

namespace CastingRadar.Domain.Entities;

public class CastingCall
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string SourceUrl { get; private set; } = string.Empty;
    public string SourceName { get; private set; } = string.Empty;
    public CastingType Type { get; private set; }
    public SourceRegion Region { get; private set; }
    public string? Location { get; private set; }
    public DateTime? Deadline { get; private set; }
    public bool IsPaid { get; private set; }
    public string? RequiredGender { get; private set; }
    public string? AgeRange { get; private set; }
    public string? Requirements { get; private set; }
    public bool IsFavorite { get; private set; }
    public bool IsApplied { get; private set; }
    public bool IsHidden { get; private set; }
    public DateTime ScrapedAt { get; private set; }
    public string ContentHash { get; private set; } = string.Empty;

    private CastingCall() { }

    public static CastingCall Create(
        string title,
        string description,
        string sourceUrl,
        string sourceName,
        CastingType type,
        SourceRegion region,
        string? location = null,
        DateTime? deadline = null,
        bool isPaid = false,
        string? requiredGender = null,
        string? ageRange = null,
        string? requirements = null)
    {
        var hash = ComputeHash(title, sourceUrl);
        return new CastingCall
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            SourceUrl = sourceUrl,
            SourceName = sourceName,
            Type = type,
            Region = region,
            Location = location,
            Deadline = deadline,
            IsPaid = isPaid,
            RequiredGender = requiredGender,
            AgeRange = ageRange,
            Requirements = requirements,
            IsFavorite = false,
            IsApplied = false,
            ScrapedAt = DateTime.UtcNow,
            ContentHash = hash
        };
    }

    public void ToggleFavorite() => IsFavorite = !IsFavorite;

    public void MarkAsApplied() => IsApplied = true;
    public void UnmarkAsApplied() => IsApplied = false;
    public void ToggleHidden() => IsHidden = !IsHidden;

    public static string ComputeHash(string title, string sourceUrl)
    {
        var input = $"{title.Trim().ToLowerInvariant()}|{sourceUrl.Trim().ToLowerInvariant()}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
