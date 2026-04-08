using CastingRadar.Domain.Entities;

namespace CastingRadar.Application.Interfaces;

public interface ISourceRepository
{
    Task<IEnumerable<Source>> GetAllAsync(CancellationToken ct = default);
    Task<Source?> GetByNameAsync(string name, CancellationToken ct = default);
    Task AddAsync(Source source, CancellationToken ct = default);
    Task UpsertAsync(Source source, CancellationToken ct = default);
    Task UpdateAsync(Source source, CancellationToken ct = default);
    Task DeleteAsync(string name, CancellationToken ct = default);
}
