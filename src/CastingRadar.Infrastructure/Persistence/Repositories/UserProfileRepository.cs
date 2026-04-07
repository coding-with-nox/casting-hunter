using CastingRadar.Application.Interfaces;
using CastingRadar.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CastingRadar.Infrastructure.Persistence.Repositories;

public class UserProfileRepository(CastingRadarDbContext db) : IUserProfileRepository
{
    public async Task<UserProfile> GetAsync(CancellationToken ct = default)
    {
        var profile = await db.UserProfiles.FirstOrDefaultAsync(ct);
        if (profile is null)
        {
            profile = UserProfile.CreateDefault();
            db.UserProfiles.Add(profile);
            await db.SaveChangesAsync(ct);
        }
        return profile;
    }

    public async Task UpdateAsync(UserProfile profile, CancellationToken ct = default)
    {
        db.UserProfiles.Update(profile);
        await db.SaveChangesAsync(ct);
    }
}
