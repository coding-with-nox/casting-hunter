using CastingRadar.Application.Interfaces;
using CastingRadar.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CastingRadar.Infrastructure.Persistence.Repositories;

public class TeatroContactRepository(CastingRadarDbContext db) : ITeatroContactRepository
{
    public async Task<IEnumerable<TeatroContact>> GetAllAsync(CancellationToken ct = default) =>
        await db.TeatroContacts.OrderBy(c => c.Regione).ThenBy(c => c.TeatroName).ToListAsync(ct);

    public async Task<IEnumerable<TeatroContact>> GetByRegioneAsync(string regione, CancellationToken ct = default) =>
        await db.TeatroContacts
            .Where(c => c.Regione == regione)
            .OrderBy(c => c.TeatroName)
            .ToListAsync(ct);

    public async Task<TeatroContact?> GetByNameAsync(string teatroName, CancellationToken ct = default) =>
        await db.TeatroContacts.FirstOrDefaultAsync(c => c.TeatroName == teatroName, ct);

    public async Task AddAsync(TeatroContact contact, CancellationToken ct = default)
    {
        db.TeatroContacts.Add(contact);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(TeatroContact contact, CancellationToken ct = default)
    {
        db.TeatroContacts.Update(contact);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(string teatroName, CancellationToken ct = default)
    {
        var entity = await db.TeatroContacts.FirstOrDefaultAsync(c => c.TeatroName == teatroName, ct);
        if (entity is not null)
        {
            db.TeatroContacts.Remove(entity);
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task UpsertAsync(TeatroContact contact, CancellationToken ct = default)
    {
        var existing = await db.TeatroContacts
            .FirstOrDefaultAsync(c => c.TeatroName == contact.TeatroName, ct);

        if (existing is null)
        {
            db.TeatroContacts.Add(contact);
        }
        else
        {
            existing.UpdateScrapeResult(
                contact.Email,
                contact.Phone,
                contact.Address,
                contact.ContactPageUrl,
                contact.Notes);
        }

        await db.SaveChangesAsync(ct);
    }
}
