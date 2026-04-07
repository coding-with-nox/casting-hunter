using CastingRadar.Domain.Entities;
using CastingRadar.Domain.Enums;

namespace CastingRadar.Application.DTOs;

public record UserProfileDto(
    CastingType[] PreferredTypes,
    SourceRegion[] PreferredRegions,
    int? ScenicAge,
    string Gender,
    string? TelegramChatId)
{
    public static UserProfileDto FromEntity(UserProfile p) =>
        new(p.PreferredTypes, p.PreferredRegions, p.ScenicAge, p.Gender, p.TelegramChatId);
}
