using CastingRadar.Application.DTOs;
using CastingRadar.Application.Interfaces;
using CastingRadar.Application.UseCases.ScrapeBandiPhaseOne;
using CastingRadar.Domain.Entities;

namespace CastingRadar.Api.Endpoints;

public static class BandiEndpoints
{
    public static IEndpointRouteBuilder MapBandiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bandi");

        group.MapGet("/", async (IBandoRepository repo, string? userStatus, CancellationToken ct) =>
        {
            var bandi = await repo.GetAllAsync(ct);
            if (!string.IsNullOrWhiteSpace(userStatus))
                bandi = bandi.Where(b => string.Equals(b.UserStatus, userStatus, StringComparison.OrdinalIgnoreCase));
            return Results.Ok(bandi.Select(BandoDto.FromEntity));
        });

        group.MapPatch("/{id:guid}/user-status", async (
            Guid id,
            SetUserStatusRequest req,
            IBandoRepository repo,
            CancellationToken ct) =>
        {
            var bando = await repo.GetByIdAsync(id, ct);
            if (bando is null) return Results.NotFound();
            // null = reset, "Considerato", "Escluso"
            var status = req.Status?.Trim();
            if (status is not null && status is not "Considerato" and not "Escluso")
                return Results.BadRequest("Status deve essere null, 'Considerato' o 'Escluso'");
            bando.SetUserStatus(status);
            await repo.UpdateAsync(bando, ct);
            return Results.Ok(BandoDto.FromEntity(bando));
        });

        group.MapGet("/{id:guid}", async (Guid id, IBandoRepository repo, CancellationToken ct) =>
        {
            var bando = await repo.GetByIdAsync(id, ct);
            return bando is null
                ? Results.NotFound()
                : Results.Ok(BandoDto.FromEntity(bando));
        });

        group.MapGet("/sources", async (
            IBandoSourceRepository repo,
            string? regione,
            CancellationToken ct) =>
        {
            var sources = await repo.GetAllAsync(ct);
            if (!string.IsNullOrWhiteSpace(regione))
                sources = sources.Where(s => string.Equals(s.Regione, regione, StringComparison.OrdinalIgnoreCase));
            return Results.Ok(sources.Select(BandoSourceDto.FromEntity));
        });

        group.MapPatch("/sources/{name}/enabled", async (
            string name,
            bool enabled,
            IBandoSourceRepository repo,
            CancellationToken ct) =>
        {
            var source = await repo.GetByNameAsync(Uri.UnescapeDataString(name), ct);
            if (source is null) return Results.NotFound();
            source.SetEnabled(enabled);
            await repo.UpdateAsync(source, ct);
            return Results.NoContent();
        });

        group.MapPut("/sources/{name}", async (
            string name,
            UpdateBandoSourceRequest req,
            IBandoSourceRepository repo,
            CancellationToken ct) =>
        {
            var source = await repo.GetByNameAsync(Uri.UnescapeDataString(name), ct);
            if (source is null) return Results.NotFound();
            if (req.BaseUrl is not null)
            {
                if (!Uri.TryCreate(req.BaseUrl, UriKind.Absolute, out _))
                    return Results.BadRequest("BaseUrl non valido");
                source.SetBaseUrl(req.BaseUrl.Trim());
            }
            if (req.Regione is not null)
                source.SetRegione(req.Regione.Trim());
            await repo.UpdateAsync(source, ct);
            return Results.Ok(BandoSourceDto.FromEntity(source));
        });

        group.MapDelete("/sources/{name}", async (
            string name,
            IBandoSourceRepository repo,
            CancellationToken ct) =>
        {
            var source = await repo.GetByNameAsync(Uri.UnescapeDataString(name), ct);
            if (source is null) return Results.NotFound();
            if (source.IsOfficial && source.Priority <= 15)
                return Results.BadRequest("Le fonti ufficiali P1/P2 non possono essere eliminate, solo disattivate.");
            await repo.DeleteAsync(Uri.UnescapeDataString(name), ct);
            return Results.NoContent();
        });

        group.MapPost("/sources/curated", async (
            CreateCuratedBandoSourceRequest request,
            IBandoSourceRepository repo,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.BaseUrl))
            {
                return Results.BadRequest("Name e BaseUrl sono obbligatori");
            }

            if (!Uri.TryCreate(request.BaseUrl, UriKind.Absolute, out _))
            {
                return Results.BadRequest("BaseUrl non valido");
            }

            var existing = await repo.GetByNameAsync(request.Name.Trim(), ct);
            if (existing is not null)
            {
                return Results.Conflict($"Fonte '{request.Name}' gia presente");
            }

            var source = BandoSource.Create(
                name: request.Name.Trim(),
                category: "P3 - Associazioni e organismi curati",
                baseUrl: request.BaseUrl.Trim(),
                priority: request.Priority is > 15 ? request.Priority.Value : 20,
                isOfficial: request.IsOfficial ?? false,
                isEnabled: true);

            await repo.AddAsync(source, ct);
            return Results.Created($"/api/bandi/sources/{Uri.EscapeDataString(source.Name)}", BandoSourceDto.FromEntity(source));
        });

        group.MapPost("/scrape-p1", async (ScrapeBandiPhaseOneHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(1, 4, ct);
            return Results.Ok(BandoScrapeResultDto.Create(
                totalFound: result.TotalFound,
                totalEligible: result.TotalEligible,
                totalNew: result.TotalNew,
                sources: result.Sources));
        });

        group.MapPost("/scrape-p2", async (ScrapeBandiPhaseOneHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(10, 19, ct);
            return Results.Ok(BandoScrapeResultDto.Create(
                totalFound: result.TotalFound,
                totalEligible: result.TotalEligible,
                totalNew: result.TotalNew,
                sources: result.Sources));
        });

        group.MapPost("/scrape-p3", async (ScrapeBandiPhaseOneHandler handler, CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(20, 99, ct);
            return Results.Ok(BandoScrapeResultDto.Create(
                totalFound: result.TotalFound,
                totalEligible: result.TotalEligible,
                totalNew: result.TotalNew,
                sources: result.Sources));
        });

        group.MapGet("/plan", () =>
        {
            var plan = new BandiPlanDto(
                Status: "Planning + API base",
                Summary: "Roadmap iniziale per integrare i bandi di concorso artistici con fonti ufficiali, parsing PDF e classificazione per ente.",
                PriorityPlan:
                [
                    new("Definire il perimetro artistico", "Bloccare whitelist e blacklist dei ruoli per separare performer e profili non artistici."),
                    new("Integrare le fonti nazionali ufficiali", "Partire da inPA, Gazzetta Ufficiale e MiC Spettacolo per avere copertura pubblica stabile."),
                    new("Aggiungere parsing PDF e OCR fallback", "Estrarre testo dagli allegati PDF e usare OCR solo quando il testo non e leggibile."),
                    new("Creare il registro fonti per categoria ente", "Classificare le fonti in PA, teatri, fondazioni, associazioni e accademie."),
                    new("Costruire classificazione e deduplica", "Unire bandi duplicati tra ente, Gazzetta e allegati PDF."),
                    new("Attivare scraper verticali dei teatri", "Integrare Scala, Opera di Roma, TCBO, San Carlo, Teatro Massimo e teatri stabili."),
                    new("Gestire i bandi a bassa confidenza", "Introdurre revisione manuale per i risultati dubbi."),
                    new("Aprire alla rete associazioni curate", "Espandere solo tramite whitelist ufficiale o derivata da elenchi MiC/FNSV.")
                ],
                ExtractionFlow:
                [
                    "Scaricare liste HTML ufficiali e pagine dettaglio.",
                    "Raccogliere titolo, ente, data, scadenza, link candidatura e allegati.",
                    "Estrarre testo dai PDF allegati e usare OCR quando manca testo leggibile.",
                    "Applicare filtro artistico su ente, sezione del sito e contenuto del bando.",
                    "Salvare punteggio di confidenza e mandare in revisione i casi ambigui."
                ],
                IssuerTypes:
                [
                    "PA: ministeri, enti locali, accademie, universita, enti vigilati",
                    "Teatri e fondazioni pubbliche o partecipate",
                    "Fondazioni lirico-sinfoniche",
                    "Associazioni e fondazioni private o non profit"
                ],
                SourceGroups:
                [
                    new("P1 - Fonti nazionali", [
                        "inPA - bandi e avvisi",
                        "Gazzetta Ufficiale - 4a serie concorsi",
                        "MiC Spettacolo",
                        "MiC - fondazioni lirico-sinfoniche ed elenchi organismi"
                    ]),
                    new("P2 - Teatri e fondazioni ad alta resa", [
                        "Teatro alla Scala",
                        "Teatro dell Opera di Roma",
                        "Teatro Comunale di Bologna",
                        "Teatro di San Carlo",
                        "Teatro Massimo",
                        "Teatro Stabile di Torino"
                    ]),
                    new("P3 - Associazioni e organismi curati", [
                        "Associazioni culturali in whitelist manuale",
                        "Fondazioni private con sezioni bandi o audizioni ufficiali",
                        "Scuole e accademie con avvisi pubblici di selezione artistica"
                    ])
                ]);

            return Results.Ok(plan);
        });

        return app;
    }
}

public record CreateCuratedBandoSourceRequest(
    string Name,
    string BaseUrl,
    int? Priority,
    bool? IsOfficial);

public record UpdateBandoSourceRequest(string? BaseUrl, string? Regione);
public record SetUserStatusRequest(string? Status);
