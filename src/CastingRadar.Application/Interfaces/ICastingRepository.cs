using CastingRadar.Domain.Entities;
using CastingRadar.Domain.ValueObjects;

namespace CastingRadar.Application.Interfaces;

public interface ICastingRepository
{
    Task<IEnumerable<CastingCall>> GetAllAsync(ScraperFilter? filter = null, CancellationToken ct = default);
    Task<CastingCall?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByHashAsync(string contentHash, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<CastingCall> calls, CancellationToken ct = default);
    Task UpdateAsync(CastingCall call, CancellationToken ct = default);
    Task<int> CountTodayAsync(CancellationToken ct = default);
    Task<Dictionary<string, int>> CountBySourceAsync(CancellationToken ct = default);
}
