using CastingRadar.Application.DTOs;
using CastingRadar.Application.Interfaces;
using CastingRadar.Domain.Enums;

namespace CastingRadar.Application.UseCases.UpdateUserProfile;

public record UpdateUserProfileRequest(
    CastingType[]? PreferredTypes,
    SourceRegion[]? PreferredRegions,
    int? ScenicAge,
    string? Gender,
    string? TelegramChatId);

public class UpdateUserProfileHandler(IUserProfileRepository repository)
{
    public async Task<UserProfileDto> HandleAsync(UpdateUserProfileRequest request, CancellationToken ct = default)
    {
        var profile = await repository.GetAsync(ct);
        profile.Update(request.PreferredTypes, request.PreferredRegions, request.ScenicAge, request.Gender, request.TelegramChatId);
        await repository.UpdateAsync(profile, ct);
        return UserProfileDto.FromEntity(profile);
    }
}
