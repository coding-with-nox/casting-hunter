using CastingRadar.Application.Interfaces;
using CastingRadar.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CastingRadar.Infrastructure.Persistence.Repositories;

public class BandoSourceRepository(CastingRadarDbContext db) : IBandoSourceRepository
{
    public async Task<IEnumerable<BandoSource>> GetAllAsync(CancellationToken ct = default) =>
        await db.BandoSources
            .OrderBy(s => s.Priority)
            .ThenBy(s => s.Name)
            .ToListAsync(ct);

    public async Task<BandoSource?> GetByNameAsync(string name, CancellationToken ct = default) =>
        await db.BandoSources.FirstOrDefaultAsync(s => s.Name == name, ct);

    public async Task AddAsync(BandoSource source, CancellationToken ct = default)
    {
        db.BandoSources.Add(source);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(BandoSource source, CancellationToken ct = default)
    {
        db.BandoSources.Update(source);
        await db.SaveChangesAsync(ct);
    }
}
