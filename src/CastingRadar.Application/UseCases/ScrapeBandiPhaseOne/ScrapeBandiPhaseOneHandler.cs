using System.Text.RegularExpressions;
using CastingRadar.Application.Interfaces;
using CastingRadar.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CastingRadar.Application.UseCases.ScrapeBandiPhaseOne;

public class ScrapeBandiPhaseOneHandler(
    IEnumerable<IBandoScraperStrategy> scrapers,
    IBandoRepository bandoRepository,
    IBandoSourceRepository sourceRepository,
    ILogger<ScrapeBandiPhaseOneHandler> logger)
{
    private static readonly string[] ArtisticKeywords =
    [
        "attore", "attrice", "casting", "audizione", "audizioni", "artista del coro",
        "coro", "orchestra", "musicista", "cantante", "soprano", "tenore", "baritono",
        "danza", "danzatore", "danzatrice", "ballerino", "ballerina", "mimo", "regista",
        "scuola per attori", "selezione artistica", "performer", "maestro collaboratore",
        "strumentista", "violino", "viola", "violoncello", "contrabbasso", "flauto",
        "oboe", "clarinetto", "fagotto", "corno", "tromba", "trombone", "arpa", "percussioni"
    ];

    private static readonly string[] ExcludedKeywords =
    [
        "amministrativo", "amministrativa", "contabile", "ragioneria", "biglietteria",
        "fornitura", "forniture", "appalto", "appalti", "ict", "informatico", "developer",
        "hr", "risorse umane", "segreteria", "portierato", "manutenzione", "custode"
    ];

    public async Task<BandoScrapeResult> HandleAsync(CancellationToken ct = default)
    {
        var enabledSources = (await sourceRepository.GetAllAsync(ct))
            .Where(source => source.IsEnabled && source.Priority <= 4)
            .ToList();

        var scraperMap = scrapers.ToDictionary(scraper => scraper.SourceName, StringComparer.OrdinalIgnoreCase);
        var touchedSources = new List<string>();
        var totalFound = 0;
        var totalEligible = 0;
        var totalNew = 0;

        foreach (var source in enabledSources)
        {
            if (!scraperMap.TryGetValue(source.Name, out var scraper))
            {
                continue;
            }

            logger.LogInformation("Scraping bandi source {SourceName}", source.Name);

            var items = (await scraper.ScrapeAsync(source, ct)).ToList();
            touchedSources.Add(source.Name);
            totalFound += items.Count;

            foreach (var item in items)
            {
                if (!IsArtistic(item.Title, item.BodyText))
                {
                    continue;
                }

                totalEligible++;

                var issuerName = item.IssuerName ?? InferIssuerName(source.Name, item.Title, item.BodyText);
                var discipline = InferDiscipline(item.Title, item.BodyText);
                var role = InferRole(item.Title, item.BodyText);
                var confidenceScore = CalculateConfidence(item.Title, item.BodyText, source);
                var status = InferStatus(item.Deadline, confidenceScore);

                var bando = Bando.Create(
                    title: CleanText(item.Title),
                    issuerName: issuerName,
                    issuerType: InferIssuerType(source),
                    sourceName: source.Name,
                    sourceUrl: item.SourceUrl,
                    bodyText: CleanText(item.BodyText),
                    isPublic: source.Category.Contains("P1", StringComparison.OrdinalIgnoreCase),
                    confidenceScore: confidenceScore,
                    status: status,
                    applicationUrl: item.ApplicationUrl,
                    publishedAt: item.PublishedAt,
                    deadline: item.Deadline,
                    location: item.Location,
                    discipline: discipline,
                    role: role);

                if (await bandoRepository.ExistsByHashAsync(bando.ContentHash, ct))
                {
                    continue;
                }

                await bandoRepository.AddAsync(bando, ct);
                totalNew++;
            }
        }

        return new BandoScrapeResult(totalFound, totalEligible, totalNew, touchedSources);
    }

    private static bool IsArtistic(string title, string bodyText)
    {
        var text = $"{title} {bodyText}".ToLowerInvariant();
        return ArtisticKeywords.Any(keyword => text.Contains(keyword))
            && !ExcludedKeywords.Any(keyword => text.Contains(keyword));
    }

    private static string InferIssuerType(BandoSource source)
    {
        if (source.Name.Contains("MiC", StringComparison.OrdinalIgnoreCase) ||
            source.Name.Contains("inPA", StringComparison.OrdinalIgnoreCase) ||
            source.Name.Contains("Gazzetta", StringComparison.OrdinalIgnoreCase))
        {
            return "PA";
        }

        if (source.Name.Contains("fondazioni", StringComparison.OrdinalIgnoreCase))
        {
            return "Fondazione lirico-sinfonica";
        }

        return "Teatro o organismo culturale";
    }

    private static string InferIssuerName(string sourceName, string title, string bodyText)
    {
        if (sourceName.Contains("Gazzetta", StringComparison.OrdinalIgnoreCase))
        {
            var match = Regex.Match($"{title} {bodyText}", @"(?:presso|per il|per la|presso la)\s+([A-Z][^.:\n]{4,80})", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return CleanText(match.Groups[1].Value);
            }
        }

        return sourceName;
    }

    private static string? InferDiscipline(string title, string bodyText)
    {
        var text = $"{title} {bodyText}".ToLowerInvariant();
        if (text.Contains("danza") || text.Contains("ballo")) return "Danza";
        if (text.Contains("orchestra") || text.Contains("music") || text.Contains("coro")) return "Musica";
        if (text.Contains("attore") || text.Contains("attrice") || text.Contains("teatro")) return "Teatro";
        return "Spettacolo";
    }

    private static string? InferRole(string title, string bodyText)
    {
        var text = $"{title} {bodyText}".ToLowerInvariant();
        var role = ArtisticKeywords.FirstOrDefault(keyword => text.Contains(keyword));
        return role is null ? null : CleanText(role);
    }

    private static decimal CalculateConfidence(string title, string bodyText, BandoSource source)
    {
        decimal score = source.IsOfficial ? 0.55m : 0.35m;
        var text = $"{title} {bodyText}".ToLowerInvariant();

        foreach (var keyword in ArtisticKeywords)
        {
            if (text.Contains(keyword))
            {
                score += 0.08m;
            }
        }

        if (text.Contains("audizione") || text.Contains("concorso"))
        {
            score += 0.10m;
        }

        if (ExcludedKeywords.Any(keyword => text.Contains(keyword)))
        {
            score -= 0.40m;
        }

        return Math.Clamp(score, 0.05m, 0.99m);
    }

    private static string InferStatus(DateTime? deadline, decimal confidenceScore)
    {
        if (deadline.HasValue && deadline.Value.Date < DateTime.UtcNow.Date)
        {
            return "Scaduto";
        }

        if (confidenceScore < 0.70m)
        {
            return "Da rivedere";
        }

        return "Pubblicato";
    }

    private static string CleanText(string value) =>
        Regex.Replace(value, @"\s+", " ").Trim();
}

public record BandoScrapeResult(
    int TotalFound,
    int TotalEligible,
    int TotalNew,
    IReadOnlyList<string> Sources);
