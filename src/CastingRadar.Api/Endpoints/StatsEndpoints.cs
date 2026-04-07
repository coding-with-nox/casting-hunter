using CastingRadar.Application.Interfaces;

namespace CastingRadar.Api.Endpoints;

public static class StatsEndpoints
{
    public static IEndpointRouteBuilder MapStatsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/stats", async (ICastingRepository repo, CancellationToken ct) =>
        {
            var todayCount = await repo.CountTodayAsync(ct);
            var bySource = await repo.CountBySourceAsync(ct);
            var total = bySource.Values.Sum();

            return Results.Ok(new
            {
                Total = total,
                NewToday = todayCount,
                BySource = bySource
            });
        });

        return app;
    }
}
