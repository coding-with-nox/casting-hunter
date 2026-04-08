using CastingRadar.Domain.Entities;

namespace CastingRadar.Application.Interfaces;

public interface IBandoRepository
{
    Task<IEnumerable<Bando>> GetAllAsync(CancellationToken ct = default);
    Task<Bando?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByHashAsync(string contentHash, CancellationToken ct = default);
    Task AddAsync(Bando bando, CancellationToken ct = default);
}
