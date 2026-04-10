using AngleSharp.Dom;
using CastingRadar.Application.Interfaces;
using CastingRadar.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CastingRadar.Infrastructure.Scrapers.Bandi;

/// <summary>
/// Scraper generico per teatri regionali senza scraper dedicato.
/// Prova percorsi noti (/bandi, /audizioni, /concorsi, /lavora-con-noi, ecc.)
/// e raccoglie link con keyword artistiche.
/// </summary>
public class GenericTeatroBandoScraper(IHttpClientFactory httpClientFactory, ILogger<GenericTeatroBandoScraper> logger)
    : BaseBandoScraper(httpClientFactory, logger), IGenericTeatroBandoScraper
{
    public override string SourceName => "__generic_teatro__";

    private static readonly string[] SubPaths =
    [
        "/bandi", "/audizioni", "/concorsi",
        "/lavora-con-noi", "/lavora-con-noi/concorsi",
        "/it/il-teatro/lavora-con-noi", "/chi-siamo/lavora-con-noi",
        "/formazione", "/accademia", "/scuola",
    ];

    // Solo keyword che identificano inequivocabilmente un bando/selezione, NON un concerto o articolo
    private static readonly string[] TitleKeywords =
    [
        "audizione", "audizioni",
        "concorso", "concorsi",
        "selezione", "selezioni",
        "bando", "bandi",
        "ammissione",
        "corso per attori", "formazione attori",
        "danzatore", "danzatrice",
        "ballerino", "ballerina",
    ];

    protected override Task<IEnumerable<ScrapedBandoItem>> ScrapeInternalAsync(BandoSource source, CancellationToken ct)
        => ScrapeForSourceAsync(source, ct);

    public async Task<IEnumerable<ScrapedBandoItem>> ScrapeForSourceAsync(BandoSource source, CancellationToken ct)
    {
        var baseUri = new Uri(source.BaseUrl.TrimEnd('/'));
        var urlsToTry = new List<string> { source.BaseUrl };
        foreach (var sub in SubPaths)
            urlsToTry.Add($"{baseUri.Scheme}://{baseUri.Host}{sub}");

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<ScrapedBandoItem>();

        foreach (var url in urlsToTry)
        {
            if (results.Count >= 10) break;
            try
            {
                var doc = await LoadDocumentAsync(url, ct);
                var candidates = doc.QuerySelectorAll("a[href]")
                    .Select(link => new
                    {
                        Title = CleanText(link.TextContent),
                        Url = TryAbsoluteUrl(url, link.GetAttribute("href"))
                    })
                    .Where(item => !string.IsNullOrWhiteSpace(item.Title)
                                   && !string.IsNullOrWhiteSpace(item.Url)
                                   && !item.Url!.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
                                   && !item.Title.Contains('@')           // skip email-as-link-text
                                   && !item.Title.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                                   && item.Title.Length >= 10             // skip single-word / very short labels
                                   && seen.Add(item.Url!))
                    .Where(item => TitleKeywords.Any(kw => item.Title.Contains(kw, StringComparison.OrdinalIgnoreCase)))
                    .Take(8)
                    .ToList();

                foreach (var candidate in candidates)
                {
                    var bodyText = candidate.Title;
                    DateTime? deadline = null;
                    string? pdfUrl = null;
                    // candidate.Url è la pagina web di dettaglio — è il SourceUrl corretto
                    var pageUrl = candidate.Url!;

                    try
                    {
                        var detail = await LoadDocumentAsync(pageUrl, ct);
                        var dt = CleanText(detail.QuerySelector("main, article, .content, body")?.TextContent);
                        if (!string.IsNullOrWhiteSpace(dt)) bodyText = dt;
                        deadline = ExtractItalianDateFromText(bodyText);

                        // Cerca PDF nella pagina di dettaglio → diventa ApplicationUrl
                        pdfUrl = detail.QuerySelectorAll("a[href]")
                            .Select(a => TryAbsoluteUrl(pageUrl, a.GetAttribute("href")))
                            .FirstOrDefault(u => u is not null
                                && (u.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                                    || u.Contains("/pdf/", StringComparison.OrdinalIgnoreCase)
                                    || u.Contains("download", StringComparison.OrdinalIgnoreCase)));
                    }
                    catch { /* keep fallback */ }

                    results.Add(new ScrapedBandoItem(
                        Title: candidate.Title,
                        SourceUrl: pageUrl,
                        BodyText: bodyText,
                        Deadline: deadline,
                        ApplicationUrl: pdfUrl,
                        IssuerName: source.Name));
                }
            }
            catch { /* try next path */ }
        }

        return results;
    }
}
