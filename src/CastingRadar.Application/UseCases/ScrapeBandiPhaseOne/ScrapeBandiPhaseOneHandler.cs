using System.Text.RegularExpressions;
using CastingRadar.Application.Interfaces;
using CastingRadar.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Net;

namespace CastingRadar.Application.UseCases.ScrapeBandiPhaseOne;

public class ScrapeBandiPhaseOneHandler(
    IEnumerable<IBandoScraperStrategy> scrapers,
    IBandoRepository bandoRepository,
    IBandoSourceRepository sourceRepository,
    ILogger<ScrapeBandiPhaseOneHandler> logger)
{
    private static readonly Dictionary<string, decimal> HighSignalKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["audizione"] = 0.18m,
        ["audizioni"] = 0.18m,
        ["casting"] = 0.18m,
        ["attore"] = 0.18m,
        ["attrice"] = 0.18m,
        ["artista del coro"] = 0.20m,
        ["orchestra"] = 0.18m,
        ["cantante"] = 0.16m,
        ["danzatore"] = 0.18m,
        ["danzatrice"] = 0.18m,
        ["ballerino"] = 0.16m,
        ["ballerina"] = 0.16m,
        ["performer"] = 0.16m,
        ["maestro collaboratore"] = 0.18m,
        ["strumentista"] = 0.16m
    };

    private static readonly Dictionary<string, decimal> MediumSignalKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["coro"] = 0.09m,
        ["musicista"] = 0.08m,
        ["soprano"] = 0.08m,
        ["tenore"] = 0.08m,
        ["baritono"] = 0.08m,
        ["danza"] = 0.08m,
        ["mimo"] = 0.08m,
        ["regista"] = 0.08m,
        ["scuola per attori"] = 0.08m,
        ["selezione artistica"] = 0.08m,
        ["violino"] = 0.06m,
        ["viola"] = 0.06m,
        ["violoncello"] = 0.06m,
        ["contrabbasso"] = 0.06m,
        ["flauto"] = 0.06m,
        ["oboe"] = 0.06m,
        ["clarinetto"] = 0.06m,
        ["fagotto"] = 0.06m,
        ["corno"] = 0.06m,
        ["tromba"] = 0.06m,
        ["trombone"] = 0.06m,
        ["arpa"] = 0.06m,
        ["percussioni"] = 0.06m,
        ["teatro"] = 0.06m
    };

    private static readonly Dictionary<string, decimal> ReviewPenaltyKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["amministrativo"] = 0.22m,
        ["amministrativa"] = 0.22m,
        ["contabile"] = 0.20m,
        ["ragioneria"] = 0.22m,
        ["biglietteria"] = 0.22m,
        ["segreteria"] = 0.18m,
        ["portierato"] = 0.18m,
        ["custode"] = 0.18m,
        ["manutenzione"] = 0.18m,
        ["fonico"] = 0.15m,
        ["tecnico luci"] = 0.18m,
        ["macchinista"] = 0.18m,
        ["sarta"] = 0.14m,
        ["truccatore"] = 0.14m,
        ["parrucchiere"] = 0.14m
    };

    private static readonly string[] HardExcludedKeywords =
    [
        "fornitura", "forniture", "appalto", "appalti", "gara", "gare", "ict",
        "informatico", "developer", "hr", "risorse umane", "software", "sistemista"
    ];

    public Task<BandoScrapeResult> HandleAsync(CancellationToken ct = default) =>
        HandleAsync(1, 4, ct);

    public async Task<BandoScrapeResult> HandleAsync(int minPriority, int maxPriority, CancellationToken ct = default)
    {
        var enabledSources = (await sourceRepository.GetAllAsync(ct))
            .Where(source => source.IsEnabled && source.Priority >= minPriority && source.Priority <= maxPriority)
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
                var text = CleanText($"{item.Title} {item.BodyText}");
                if (!IsArtistic(text))
                {
                    continue;
                }

                totalEligible++;

                var issuerName = item.IssuerName ?? InferIssuerName(source.Name, item.Title, item.BodyText);
                var discipline = InferDiscipline(item.Title, item.BodyText);
                var role = InferRole(item.Title, item.BodyText);
                var confidenceScore = CalculateConfidence(text, source, role, discipline, item.Deadline);
                var status = InferStatus(item.Deadline, confidenceScore, role, discipline);

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

        foreach (var source in enabledSources.Where(source => !scraperMap.ContainsKey(source.Name)))
        {
            logger.LogInformation("Scraping curated bando source {SourceName}", source.Name);
            var items = (await ScrapeCuratedSourceAsync(source, ct)).ToList();
            touchedSources.Add(source.Name);
            totalFound += items.Count;

            foreach (var item in items)
            {
                var text = CleanText($"{item.Title} {item.BodyText}");
                if (!IsArtistic(text))
                {
                    continue;
                }

                totalEligible++;

                var issuerName = item.IssuerName ?? source.Name;
                var discipline = InferDiscipline(item.Title, item.BodyText);
                var role = InferRole(item.Title, item.BodyText);
                var confidenceScore = CalculateConfidence(text, source, role, discipline, item.Deadline);
                var status = InferStatus(item.Deadline, confidenceScore, role, discipline);

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

    private static bool IsArtistic(string text)
    {
        var normalized = text.ToLowerInvariant();
        if (HardExcludedKeywords.Any(keyword => normalized.Contains(keyword)))
        {
            return false;
        }

        var score = GetPositiveSignalScore(normalized) - GetPenaltyScore(normalized);
        return score >= 0.10m;
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
        var role = HighSignalKeywords.Keys.Concat(MediumSignalKeywords.Keys)
            .FirstOrDefault(keyword => text.Contains(keyword));
        return role is null ? null : CleanText(role);
    }

    private static decimal CalculateConfidence(
        string text,
        BandoSource source,
        string? role,
        string? discipline,
        DateTime? deadline)
    {
        decimal score = source.IsOfficial ? 0.48m : 0.30m;
        var normalized = text.ToLowerInvariant();

        score += GetPositiveSignalScore(normalized);
        score -= GetPenaltyScore(normalized);

        if (normalized.Contains("concorso") || normalized.Contains("selezione"))
        {
            score += 0.10m;
        }

        if (role is not null)
        {
            score += 0.05m;
        }

        if (discipline is not null && !string.Equals(discipline, "Spettacolo", StringComparison.OrdinalIgnoreCase))
        {
            score += 0.04m;
        }

        if (deadline.HasValue)
        {
            score += 0.03m;
        }

        if (HardExcludedKeywords.Any(keyword => normalized.Contains(keyword)))
        {
            score -= 0.60m;
        }

        return Math.Clamp(score, 0.05m, 0.99m);
    }

    private static string InferStatus(DateTime? deadline, decimal confidenceScore, string? role, string? discipline)
    {
        if (deadline.HasValue && deadline.Value.Date < DateTime.UtcNow.Date)
        {
            return "Scaduto";
        }

        if (confidenceScore < 0.78m ||
            role is null ||
            string.Equals(discipline, "Spettacolo", StringComparison.OrdinalIgnoreCase))
        {
            return "Da rivedere";
        }

        return "Pubblicato";
    }

    private static string CleanText(string value) =>
        Regex.Replace(value, @"\s+", " ").Trim();

    private static decimal GetPositiveSignalScore(string text)
    {
        decimal score = 0m;

        foreach (var entry in HighSignalKeywords)
        {
            if (text.Contains(entry.Key))
            {
                score += entry.Value;
            }
        }

        foreach (var entry in MediumSignalKeywords)
        {
            if (text.Contains(entry.Key))
            {
                score += entry.Value;
            }
        }

        return score;
    }

    private static decimal GetPenaltyScore(string text)
    {
        decimal score = 0m;

        foreach (var entry in ReviewPenaltyKeywords)
        {
            if (text.Contains(entry.Key))
            {
                score += entry.Value;
            }
        }

        return score;
    }

    private static async Task<IEnumerable<ScrapedBandoItem>> ScrapeCuratedSourceAsync(BandoSource source, CancellationToken ct)
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124 Safari/537.36");

        var html = await http.GetStringAsync(source.BaseUrl, ct);
        var baseUri = new Uri(source.BaseUrl);
        var results = new List<ScrapedBandoItem>();

        var linkMatches = Regex.Matches(
            html,
            @"<a\s[^>]*href=[""']([^""']+)[""'][^>]*>(.*?)</a>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        foreach (Match match in linkMatches.Cast<Match>().Take(120))
        {
            var rawLink = match.Groups[1].Value;
            var linkText = WebUtility.HtmlDecode(Regex.Replace(match.Groups[2].Value, "<.*?>", " "));
            var title = CleanText(linkText);

            if (string.IsNullOrWhiteSpace(title) || !LooksCuratedAndArtistic(title))
            {
                continue;
            }

            var link = Uri.TryCreate(rawLink, UriKind.Absolute, out var absolute)
                ? absolute.ToString()
                : new Uri(baseUri, rawLink).ToString();

            results.Add(new ScrapedBandoItem(
                Title: title,
                SourceUrl: link,
                BodyText: title,
                IssuerName: source.Name));
        }

        return results
            .DistinctBy(item => item.SourceUrl, StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .ToList();
    }

    private static bool LooksCuratedAndArtistic(string title)
    {
        var text = title.ToLowerInvariant();
        return (text.Contains("bando") || text.Contains("audizione") || text.Contains("selezione") || text.Contains("concorso"))
            && GetPositiveSignalScore(text) > 0m
            && !HardExcludedKeywords.Any(keyword => text.Contains(keyword));
    }
}

public record BandoScrapeResult(
    int TotalFound,
    int TotalEligible,
    int TotalNew,
    IReadOnlyList<string> Sources);
