using CastingRadar.Application.Interfaces;
using CastingRadar.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CastingRadar.Infrastructure.Persistence.Repositories;

public class BandoRepository(CastingRadarDbContext db) : IBandoRepository
{
    public async Task<IEnumerable<Bando>> GetAllAsync(CancellationToken ct = default) =>
        await db.Bandi
            .OrderByDescending(b => b.PublishedAt ?? b.CreatedAt)
            .ThenBy(b => b.Title)
            .ToListAsync(ct);

    public async Task<Bando?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.Bandi.FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<bool> ExistsByHashAsync(string contentHash, CancellationToken ct = default) =>
        await db.Bandi.AnyAsync(b => b.ContentHash == contentHash, ct);

    public async Task AddAsync(Bando bando, CancellationToken ct = default)
    {
        db.Bandi.Add(bando);
        await db.SaveChangesAsync(ct);
    }
}
