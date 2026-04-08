using CastingRadar.Application.DTOs;
using CastingRadar.Application.Interfaces;
using CastingRadar.Application.UseCases.ScrapeAllSources;
using CastingRadar.Domain.Entities;
using CastingRadar.Domain.Enums;
using CastingRadar.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CastingRadar.Api.Endpoints;

public static class SourceEndpoints
{
    public static IEndpointRouteBuilder MapSourceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sources");

        group.MapGet("/", async (
            ISourceRepository repo,
            IEnumerable<ICastingScraperStrategy> scrapers,
            CancellationToken ct) =>
        {
            var dbSources = (await repo.GetAllAsync(ct)).ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);

            // Registered scrapers — use DB record if available, otherwise default
            var result = scrapers.Select(s => dbSources.TryGetValue(s.SourceName, out var db)
                ? SourceStatusDto.FromEntity(db)
                : SourceStatusDto.FromScraper(s.SourceName, s.Region)).ToList();

            // Generic DB-only sources (no registered scraper)
            var registeredNames = scrapers.Select(s => s.SourceName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            result.AddRange(dbSources.Values
                .Where(s => !registeredNames.Contains(s.Name))
                .Select(SourceStatusDto.FromEntity));

            return Results.Ok(result);
        });

        group.MapPost("/scrape-all", async (ScrapeAllSourcesHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(ScraperFilter.Default, ct);
            return Results.Ok(new { result.TotalFound, result.TotalNew });
        }).RequireRateLimiting("scrape");

        group.MapPost("/", async (
            CreateSourceRequest req,
            ISourceRepository repo,
            CancellationToken ct) =>
        {
            var existing = await repo.GetByNameAsync(req.Name, ct);
            if (existing is not null) return Results.Conflict($"Source '{req.Name}' already exists");

            if (!Uri.TryCreate(req.Url, UriKind.Absolute, out _))
                return Results.BadRequest("Invalid URL format");

            if (!Enum.TryParse<SourceRegion>(req.Region, true, out var region))
                return Results.BadRequest("Invalid region. Valid values: Italy, Europe, International");

            var source = Source.Create(req.Name, region, req.Url);
            await repo.AddAsync(source, ct);
            return Results.Created($"/api/sources/{Uri.EscapeDataString(req.Name)}", SourceStatusDto.FromEntity(source));
        });

        group.MapPatch("/{name}/enabled", async (
            string name,
            bool enabled,
            ISourceRepository repo,
            CancellationToken ct) =>
        {
            var source = await repo.GetByNameAsync(name, ct);
            if (source is null) return Results.NotFound($"Source '{name}' not found");
            source.SetEnabled(enabled);
            await repo.UpdateAsync(source, ct);
            return Results.Ok();
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
        }).RequireRateLimiting("scrape");

        return app;
    }
}

public record CreateSourceRequest(string Name, string Url, string Region);
