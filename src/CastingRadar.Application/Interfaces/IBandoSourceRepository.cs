using CastingRadar.Domain.Entities;

namespace CastingRadar.Application.Interfaces;

public interface IBandoSourceRepository
{
    Task<IEnumerable<BandoSource>> GetAllAsync(CancellationToken ct = default);
    Task<BandoSource?> GetByNameAsync(string name, CancellationToken ct = default);
    Task AddAsync(BandoSource source, CancellationToken ct = default);
    Task UpdateAsync(BandoSource source, CancellationToken ct = default);
}
