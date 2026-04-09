using CastingRadar.Application.Interfaces;
using CastingRadar.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CastingRadar.Infrastructure.Persistence.Repositories;

public class BandoKeywordExclusionRepository(CastingRadarDbContext db) : IBandoKeywordExclusionRepository
{
    public async Task<IEnumerable<BandoKeywordExclusion>> GetAllAsync(CancellationToken ct = default) =>
        await db.BandoKeywordExclusions.OrderBy(e => e.Word).ToListAsync(ct);

    public async Task<bool> ExistsByWordAsync(string word, CancellationToken ct = default) =>
        await db.BandoKeywordExclusions.AnyAsync(e => e.Word == word.Trim().ToLowerInvariant(), ct);

    public async Task AddAsync(BandoKeywordExclusion exclusion, CancellationToken ct = default)
    {
        db.BandoKeywordExclusions.Add(exclusion);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(string word, CancellationToken ct = default)
    {
        var normalized = word.Trim().ToLowerInvariant();
        var entity = await db.BandoKeywordExclusions.FirstOrDefaultAsync(e => e.Word == normalized, ct);
        if (entity is not null)
        {
            db.BandoKeywordExclusions.Remove(entity);
            await db.SaveChangesAsync(ct);
        }
    }
}
