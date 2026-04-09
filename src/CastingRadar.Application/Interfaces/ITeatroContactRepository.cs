using CastingRadar.Domain.Entities;

namespace CastingRadar.Application.Interfaces;

public interface ITeatroContactRepository
{
    Task<IEnumerable<TeatroContact>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<TeatroContact>> GetByRegioneAsync(string regione, CancellationToken ct = default);
    Task<TeatroContact?> GetByNameAsync(string teatroName, CancellationToken ct = default);
    Task AddAsync(TeatroContact contact, CancellationToken ct = default);
    Task UpdateAsync(TeatroContact contact, CancellationToken ct = default);
    Task DeleteAsync(string teatroName, CancellationToken ct = default);
    Task UpsertAsync(TeatroContact contact, CancellationToken ct = default);
}
