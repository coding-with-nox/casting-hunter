using CastingRadar.Domain.Enums;

namespace CastingRadar.Domain.Entities;

public class UserProfile
{
    public int Id { get; private set; }
    public CastingType[] PreferredTypes { get; private set; } = [];
    public SourceRegion[] PreferredRegions { get; private set; } = [SourceRegion.Italy];
    public int? ScenicAge { get; private set; }
    public string Gender { get; private set; } = "female";
    public string? TelegramChatId { get; private set; }
    public DateTime LastVisitedAt { get; private set; } = DateTime.UtcNow;

    private UserProfile() { }

    public static UserProfile CreateDefault() => new() { Id = 1 };

    public void Update(
        CastingType[]? preferredTypes,
        SourceRegion[]? preferredRegions,
        int? scenicAge,
        string? gender,
        string? telegramChatId)
    {
        if (preferredTypes is not null) PreferredTypes = preferredTypes;
        if (preferredRegions is not null) PreferredRegions = preferredRegions;
        if (scenicAge is not null) ScenicAge = scenicAge;
        if (gender is not null) Gender = gender;
        if (telegramChatId is not null) TelegramChatId = telegramChatId;
    }

    public void RecordVisit() => LastVisitedAt = DateTime.UtcNow;
}
