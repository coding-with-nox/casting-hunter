using CastingRadar.Application.Interfaces;

namespace CastingRadar.Application.UseCases.MarkAsApplied;

public class UnmarkAsAppliedHandler(ICastingRepository repository)
{
    public async Task<bool> HandleAsync(Guid id, CancellationToken ct = default)
    {
        var call = await repository.GetByIdAsync(id, ct);
        if (call is null) return false;
        call.UnmarkAsApplied();
        await repository.UpdateAsync(call, ct);
        return true;
    }
}
