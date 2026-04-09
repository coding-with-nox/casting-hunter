using CastingRadar.Domain.Entities;

namespace CastingRadar.Application.Interfaces;

public interface IBandoKeywordExclusionRepository
{
    Task<IEnumerable<BandoKeywordExclusion>> GetAllAsync(CancellationToken ct = default);
    Task<bool> ExistsByWordAsync(string word, CancellationToken ct = default);
    Task AddAsync(BandoKeywordExclusion exclusion, CancellationToken ct = default);
    Task DeleteAsync(string word, CancellationToken ct = default);
}
