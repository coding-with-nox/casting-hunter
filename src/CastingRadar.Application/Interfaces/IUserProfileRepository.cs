using CastingRadar.Domain.Entities;

namespace CastingRadar.Application.Interfaces;

public interface IUserProfileRepository
{
    Task<UserProfile> GetAsync(CancellationToken ct = default);
    Task UpdateAsync(UserProfile profile, CancellationToken ct = default);
}
