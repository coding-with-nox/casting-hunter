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
        "/bandi", "/audizioni", "/concorsi", "/lavora-con-noi", "/lavora-con-noi/concorsi",
        "/it/il-teatro/lavora-con-noi", "/chi-siamo/lavora-con-noi",
        "/news", "/comunicati", "/eventi", "/il-teatro",
    ];

    private static readonly string[] TitleKeywords =
    [
        "audizione", "concorso", "selezione", "bando", "ammissione",
        "corso per attori", "formazione attori", "iscrizione",
        "coro", "orchestra", "danzatore", "ballerino", "attore", "attrice"
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
                    try
                    {
                        var detail = await LoadDocumentAsync(candidate.Url!, ct);
                        var dt = CleanText(detail.QuerySelector("main, article, .content, body")?.TextContent);
                        if (!string.IsNullOrWhiteSpace(dt)) bodyText = dt;
                        deadline = ExtractItalianDateFromText(bodyText);
                    }
                    catch { /* keep fallback */ }

                    results.Add(new ScrapedBandoItem(
                        Title: candidate.Title,
                        SourceUrl: candidate.Url!,
                        BodyText: bodyText,
                        Deadline: deadline,
                        IssuerName: source.Name));
                }
            }
            catch { /* try next path */ }
        }

        return results;
    }
}
