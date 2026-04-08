using CastingRadar.Application.DTOs;
using CastingRadar.Application.Interfaces;
using CastingRadar.Application.UseCases.ScrapeBandiPhaseOne;

namespace CastingRadar.Api.Endpoints;

public static class BandiEndpoints
{
    public static IEndpointRouteBuilder MapBandiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bandi");

        group.MapGet("/", async (IBandoRepository repo, CancellationToken ct) =>
        {
            var bandi = await repo.GetAllAsync(ct);
            return Results.Ok(bandi.Select(BandoDto.FromEntity));
        });

        group.MapGet("/{id:guid}", async (Guid id, IBandoRepository repo, CancellationToken ct) =>
        {
            var bando = await repo.GetByIdAsync(id, ct);
            return bando is null
                ? Results.NotFound()
                : Results.Ok(BandoDto.FromEntity(bando));
        });

        group.MapGet("/sources", async (IBandoSourceRepository repo, CancellationToken ct) =>
        {
            var sources = await repo.GetAllAsync(ct);
            return Results.Ok(sources.Select(BandoSourceDto.FromEntity));
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
            var result = await handler.HandleAsync(10, 15, ct);
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
