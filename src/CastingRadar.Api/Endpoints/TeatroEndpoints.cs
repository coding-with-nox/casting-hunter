using CastingRadar.Application.Interfaces;
using CastingRadar.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace CastingRadar.Api.Endpoints;

public static class TeatroEndpoints
{
    public static IEndpointRouteBuilder MapTeatroEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/teatri");

        // ── Contacts ────────────────────────────────────────────────────────────

        group.MapGet("/contatti", async (
            ITeatroContactRepository repo,
            string? regione,
            CancellationToken ct) =>
        {
            var contacts = string.IsNullOrWhiteSpace(regione)
                ? await repo.GetAllAsync(ct)
                : await repo.GetByRegioneAsync(regione, ct);
            return Results.Ok(contacts.Select(TeatroContactDto.FromEntity));
        });

        group.MapGet("/contatti/{name}", async (
            string name,
            ITeatroContactRepository repo,
            CancellationToken ct) =>
        {
            var contact = await repo.GetByNameAsync(Uri.UnescapeDataString(name), ct);
            return contact is null
                ? Results.NotFound()
                : Results.Ok(TeatroContactDto.FromEntity(contact));
        });

        group.MapPut("/contatti/{name}", async (
            string name,
            [FromBody] UpdateTeatroContactRequest req,
            ITeatroContactRepository repo,
            CancellationToken ct) =>
        {
            var contact = await repo.GetByNameAsync(Uri.UnescapeDataString(name), ct);
            if (contact is null) return Results.NotFound();
            contact.ManualUpdate(req.Website, req.Email, req.Phone, req.Address, req.Notes);
            await repo.UpdateAsync(contact, ct);
            return Results.Ok(TeatroContactDto.FromEntity(contact));
        });

        group.MapDelete("/contatti/{name}", async (
            string name,
            ITeatroContactRepository repo,
            CancellationToken ct) =>
        {
            await repo.DeleteAsync(Uri.UnescapeDataString(name), ct);
            return Results.NoContent();
        });

        // ── Scrape ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Scrapes contact info for all theater BandoSources.
        /// Optional ?regione=Toscana to limit to a region.
        /// Optional ?category=P2 to limit by category prefix.
        /// </summary>
        group.MapPost("/contatti/scrape", async (
            ITeatroContactScraper scraper,
            ITeatroContactRepository contactRepo,
            IBandoSourceRepository sourceRepo,
            string? regione,
            string? category,
            CancellationToken ct) =>
        {
            var allSources = await sourceRepo.GetAllAsync(ct);

            // Only P2/P3 teatro sources (not P1 national/comuni)
            var sources = allSources
                .Where(s => s.IsEnabled
                    && (s.Category.Contains("P2", StringComparison.OrdinalIgnoreCase)
                        || s.Category.Contains("P3", StringComparison.OrdinalIgnoreCase))
                    && !s.Category.Contains("comunali", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!string.IsNullOrWhiteSpace(regione))
                sources = sources.Where(s => string.Equals(s.Regione, regione, StringComparison.OrdinalIgnoreCase)).ToList();

            if (!string.IsNullOrWhiteSpace(category))
                sources = sources.Where(s => s.Category.Contains(category, StringComparison.OrdinalIgnoreCase)).ToList();

            var results = new List<object>();

            foreach (var source in sources)
            {
                if (ct.IsCancellationRequested) break;

                // Use manually-set website override if present
                var existing = await contactRepo.GetByNameAsync(source.Name, ct);
                var websiteOverride = existing?.Website;
                var useOverride = !string.IsNullOrWhiteSpace(websiteOverride)
                    && !string.Equals(websiteOverride, source.BaseUrl, StringComparison.OrdinalIgnoreCase);

                var result = useOverride
                    ? await scraper.ScrapeByUrlAsync(source.Name, websiteOverride!, ct)
                    : await scraper.ScrapeAsync(source, ct);

                var contact = TeatroContact.Create(
                    teatroName: source.Name,
                    regione: source.Regione,
                    website: useOverride ? websiteOverride! : source.BaseUrl,
                    email: result.Email,
                    phone: result.Phone,
                    address: result.Address,
                    contactPageUrl: result.ContactPageUrl,
                    notes: result.Error ?? result.Notes);

                await contactRepo.UpsertAsync(contact, ct);

                results.Add(new
                {
                    teatro = source.Name,
                    regione = source.Regione,
                    email = result.Email,
                    phone = result.Phone,
                    address = result.Address,
                    error = result.Error,
                });
            }

            return Results.Ok(new { scraped = results.Count, results });
        }).RequireRateLimiting("scrape");

        // ── Regioni disponibili ──────────────────────────────────────────────────

        group.MapGet("/regioni", async (
            IBandoSourceRepository sourceRepo,
            CancellationToken ct) =>
        {
            var sources = await sourceRepo.GetAllAsync(ct);
            var regioni = sources
                .Where(s => s.Regione is not null
                    && (s.Category.Contains("P2") || s.Category.Contains("P3"))
                    && !s.Category.Contains("comunali"))
                .Select(s => s.Regione!)
                .Distinct()
                .OrderBy(r => r)
                .ToList();
            return Results.Ok(regioni);
        });

        return app;
    }
}

public record TeatroContactDto(
    string TeatroName,
    string? Regione,
    string? Website,
    string? Email,
    string? Phone,
    string? Address,
    string? ContactPageUrl,
    string? Notes,
    string? ScrapedAt)
{
    public static TeatroContactDto FromEntity(TeatroContact c) => new(
        c.TeatroName,
        c.Regione,
        c.Website,
        c.Email,
        c.Phone,
        c.Address,
        c.ContactPageUrl,
        c.Notes,
        c.ScrapedAt?.ToString("O"));
}

public record UpdateTeatroContactRequest(
    string? Website,
    string? Email,
    string? Phone,
    string? Address,
    string? Notes);
