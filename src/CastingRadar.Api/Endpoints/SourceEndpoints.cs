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

            // Registered scrapers — use DB record if available (mark as hasCustomScraper=true)
            var result = scrapers.Select(s => dbSources.TryGetValue(s.SourceName, out var db)
                ? SourceStatusDto.FromEntity(db, hasCustomScraper: true)
                : SourceStatusDto.FromScraper(s.SourceName, s.Region)).ToList();

            // Generic DB-only sources (no registered scraper)
            var registeredNames = scrapers.Select(s => s.SourceName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            result.AddRange(dbSources.Values
                .Where(s => !registeredNames.Contains(s.Name))
                .Select(s => SourceStatusDto.FromEntity(s)));

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

        group.MapDelete("/{name}", async (
            string name,
            ISourceRepository repo,
            IEnumerable<ICastingScraperStrategy> scrapers,
            CancellationToken ct) =>
        {
            if (scrapers.Any(s => s.SourceName.Equals(name, StringComparison.OrdinalIgnoreCase)))
                return Results.BadRequest("I siti integrati non possono essere eliminati, solo disattivati.");
            await repo.DeleteAsync(name, ct);
            return Results.NoContent();
        });

        group.MapPatch("/{name}/enabled", async (
            string name,
            bool enabled,
            ISourceRepository repo,
            IEnumerable<ICastingScraperStrategy> scrapers,
            CancellationToken ct) =>
        {
            var source = await repo.GetByNameAsync(name, ct);
            if (source is null)
            {
                // Built-in scraper with no DB record yet — create it on first toggle
                var scraper = scrapers.FirstOrDefault(s => s.SourceName.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (scraper is null) return Results.NotFound($"Source '{name}' not found");
                source = Source.Create(scraper.SourceName, scraper.Region, isEnabled: enabled);
                await repo.AddAsync(source, ct);
            }
            else
            {
                source.SetEnabled(enabled);
                await repo.UpdateAsync(source, ct);
            }
            return Results.NoContent();
        });

        group.MapPut("/{name}", async (
            string name,
            UpdateSourceRequest req,
            ISourceRepository repo,
            IEnumerable<ICastingScraperStrategy> scrapers,
            CancellationToken ct) =>
        {
            var source = await repo.GetByNameAsync(name, ct);
            if (source is null)
            {
                // Built-in with no record yet
                var scraper = scrapers.FirstOrDefault(s => s.SourceName.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (scraper is null) return Results.NotFound($"Source '{name}' not found");
                source = Source.Create(scraper.SourceName, scraper.Region);
                await repo.AddAsync(source, ct);
            }
            if (req.Url is not null)
            {
                if (!Uri.TryCreate(req.Url, UriKind.Absolute, out _))
                    return Results.BadRequest("Invalid URL format");
                source.SetUrl(req.Url);
            }
            if (req.Region is not null && Enum.TryParse<SourceRegion>(req.Region, true, out var region))
                source.SetRegion(region);
            await repo.UpdateAsync(source, ct);
            return Results.Ok(SourceStatusDto.FromEntity(source,
                scrapers.Any(s => s.SourceName.Equals(name, StringComparison.OrdinalIgnoreCase))));
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
public record UpdateSourceRequest(string? Url, string? Region);
