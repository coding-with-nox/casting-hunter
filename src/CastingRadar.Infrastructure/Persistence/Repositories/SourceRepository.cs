using CastingRadar.Application.Interfaces;
using CastingRadar.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CastingRadar.Infrastructure.Persistence.Repositories;

public class SourceRepository(CastingRadarDbContext db) : ISourceRepository
{
    public async Task<IEnumerable<Source>> GetAllAsync(CancellationToken ct = default) =>
        await db.Sources.ToListAsync(ct);

    public async Task<Source?> GetByNameAsync(string name, CancellationToken ct = default) =>
        await db.Sources.FirstOrDefaultAsync(s => s.Name == name, ct);

    public async Task AddAsync(Source source, CancellationToken ct = default)
    {
        db.Sources.Add(source);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Source source, CancellationToken ct = default)
    {
        db.Sources.Update(source);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpsertAsync(Source source, CancellationToken ct = default)
    {
        var existing = await db.Sources.FirstOrDefaultAsync(s => s.Name == source.Name, ct);
        if (existing is null)
            db.Sources.Add(source);
        else
            db.Sources.Update(source);
        await db.SaveChangesAsync(ct);
    }
}
