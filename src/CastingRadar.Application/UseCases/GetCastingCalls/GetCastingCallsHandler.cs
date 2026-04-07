using CastingRadar.Application.DTOs;
using CastingRadar.Application.Interfaces;
using CastingRadar.Domain.ValueObjects;

namespace CastingRadar.Application.UseCases.GetCastingCalls;

public class GetCastingCallsHandler(ICastingRepository repository)
{
    public async Task<IEnumerable<CastingCallDto>> HandleAsync(ScraperFilter? filter = null, CancellationToken ct = default)
    {
        var calls = await repository.GetAllAsync(filter, ct);
        return calls.Select(CastingCallDto.FromEntity);
    }
}
