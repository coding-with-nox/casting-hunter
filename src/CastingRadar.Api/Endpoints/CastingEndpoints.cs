using CastingRadar.Application.DTOs;
using CastingRadar.Application.Interfaces;
using CastingRadar.Application.UseCases.GetCastingCalls;
using CastingRadar.Application.UseCases.MarkAsApplied;
using CastingRadar.Application.UseCases.MarkAsFavorite;
using CastingRadar.Domain.Enums;
using CastingRadar.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace CastingRadar.Api.Endpoints;

public static class CastingEndpoints
{
    public static IEndpointRouteBuilder MapCastingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/castings");

        group.MapGet("/", async (
            [FromQuery] string? keywords,
            [FromQuery] string? types,
            [FromQuery] string? regions,
            [FromQuery] bool? onlyPaid,
            [FromQuery] string? gender,
            GetCastingCallsHandler handler,
            CancellationToken ct) =>
        {
            var filter = new ScraperFilter(
                Keywords: keywords?.Split(',', StringSplitOptions.RemoveEmptyEntries),
                Types: types?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                             .Select(t => Enum.Parse<CastingType>(t, true)).ToArray(),
                Regions: regions?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(r => Enum.Parse<SourceRegion>(r, true)).ToArray(),
                OnlyPaid: onlyPaid ?? false,
                GenderFilter: gender,
                MinAge: null,
                MaxAge: null);

            var results = await handler.HandleAsync(filter, ct);
            return Results.Ok(results);
        });

        group.MapGet("/{id:guid}", async (Guid id, ICastingRepository repo, CancellationToken ct) =>
        {
            var call = await repo.GetByIdAsync(id, ct);
            return call is null ? Results.NotFound() : Results.Ok(CastingCallDto.FromEntity(call));
        });

        group.MapPost("/{id:guid}/favorite", async (Guid id, MarkAsFavoriteHandler handler, CancellationToken ct) =>
        {
            var ok = await handler.HandleAsync(id, ct);
            return ok ? Results.Ok() : Results.NotFound();
        });

        group.MapPost("/{id:guid}/applied", async (Guid id, MarkAsAppliedHandler handler, CancellationToken ct) =>
        {
            var ok = await handler.HandleAsync(id, ct);
            return ok ? Results.Ok() : Results.NotFound();
        });

        return app;
    }
}
