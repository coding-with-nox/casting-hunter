using CastingRadar.Application.Interfaces;
using CastingRadar.Domain.Entities;
using CastingRadar.Domain.Enums;
using CastingRadar.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace CastingRadar.Infrastructure.Persistence.Repositories;

public class CastingRepository(CastingRadarDbContext db) : ICastingRepository
{
    public async Task<IEnumerable<CastingCall>> GetAllAsync(ScraperFilter? filter = null, CancellationToken ct = default)
    {
        var query = db.CastingCalls.AsQueryable();

        // Always exclude hidden unless explicitly requested
        if (filter is null || !filter.ShowHidden)
            query = query.Where(c => !c.IsHidden);

        if (filter is not null)
        {
            if (filter.Types is { Length: > 0 })
                query = query.Where(c => filter.Types.Contains(c.Type));

            if (filter.Regions is { Length: > 0 })
                query = query.Where(c => filter.Regions.Contains(c.Region));

            if (filter.OnlyPaid)
                query = query.Where(c => c.IsPaid);

            if (filter.GenderFilter is not null)
                query = query.Where(c => c.RequiredGender == null || c.RequiredGender == filter.GenderFilter);

            if (filter.Keywords is { Length: > 0 })
            {
                foreach (var kw in filter.Keywords)
                {
                    var keyword = kw;
                    query = query.Where(c => c.Title.Contains(keyword) || c.Description.Contains(keyword));
                }
            }
        }

        return await query.OrderByDescending(c => c.ScrapedAt).ToListAsync(ct);
    }

    public async Task<CastingCall?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.CastingCalls.FindAsync([id], ct);

    public async Task<bool> ExistsByHashAsync(string contentHash, CancellationToken ct = default) =>
        await db.CastingCalls.AnyAsync(c => c.ContentHash == contentHash, ct);

    public async Task AddRangeAsync(IEnumerable<CastingCall> calls, CancellationToken ct = default)
    {
        var callsList = calls.ToList();
        await db.CastingCalls.AddRangeAsync(callsList, ct);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            // Detach entities so a failed batch doesn't poison subsequent SaveChangesAsync calls
            foreach (var call in callsList)
                db.Entry(call).State = EntityState.Detached;
            throw;
        }
    }

    public async Task UpdateAsync(CastingCall call, CancellationToken ct = default)
    {
        db.CastingCalls.Update(call);
        await db.SaveChangesAsync(ct);
    }

    public async Task<int> CountTodayAsync(CancellationToken ct = default)
    {
        var today = DateTime.UtcNow.Date;
        return await db.CastingCalls.CountAsync(c => c.ScrapedAt >= today, ct);
    }

    public async Task<Dictionary<string, int>> CountBySourceAsync(CancellationToken ct = default) =>
        await db.CastingCalls
            .GroupBy(c => c.SourceName)
            .Select(g => new { Source = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Source, x => x.Count, ct);
}
