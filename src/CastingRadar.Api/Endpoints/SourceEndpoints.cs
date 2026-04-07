using CastingRadar.Application.DTOs;
using CastingRadar.Application.Interfaces;
using CastingRadar.Application.UseCases.ScrapeAllSources;
using CastingRadar.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace CastingRadar.Api.Endpoints;

public static class SourceEndpoints
{
    public static IEndpointRouteBuilder MapSourceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sources");

        group.MapGet("/", async (ISourceRepository repo, CancellationToken ct) =>
        {
            var sources = await repo.GetAllAsync(ct);
            return Results.Ok(sources.Select(SourceStatusDto.FromEntity));
        });

        group.MapPost("/scrape-all", async (ScrapeAllSourcesHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(ScraperFilter.Default, ct);
            return Results.Ok(new { result.TotalFound, result.TotalNew });
        });

        group.MapPost("/{name}/scrape", async (
            string name,
            IEnumerable<ICastingScraperStrategy> scrapers,
            ICastingRepository castingRepo,
            ISourceRepository sourceRepo,
            INotificationService notifications,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var scraper = scrapers.FirstOrDefault(s => s.SourceName.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (scraper is null) return Results.NotFound($"Source '{name}' not found");

            var handler = new ScrapeAllSourcesHandler(
                [scraper], castingRepo, sourceRepo, notifications,
                loggerFactory.CreateLogger<ScrapeAllSourcesHandler>());

            var result = await handler.HandleAsync(ScraperFilter.Default, ct);
            return Results.Ok(new { result.TotalFound, result.TotalNew });
        });

        return app;
    }
}
