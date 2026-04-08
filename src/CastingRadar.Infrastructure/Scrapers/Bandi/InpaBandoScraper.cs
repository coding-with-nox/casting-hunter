using AngleSharp.Dom;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using CastingRadar.Application.Interfaces;
using CastingRadar.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CastingRadar.Infrastructure.Scrapers.Bandi;

public class InpaBandoScraper(IHttpClientFactory httpClientFactory, ILogger<InpaBandoScraper> logger)
    : BaseBandoScraper(httpClientFactory, logger)
{
    public override string SourceName => "inPA - bandi e avvisi";

    protected override async Task<IEnumerable<ScrapedBandoItem>> ScrapeInternalAsync(BandoSource source, CancellationToken ct)
    {
        var results = new List<ScrapedBandoItem>();
        results.AddRange(await TryScrapeApiAsync(source, ct));

        if (results.Count > 0)
        {
            return results
                .DistinctBy(item => item.SourceUrl, StringComparer.OrdinalIgnoreCase)
                .Take(25)
                .ToList();
        }

        var doc = await LoadDocumentAsync(source.BaseUrl, ct);
        var links = doc.QuerySelectorAll("a[href]")
            .Select(link => new
            {
                Title = CleanText(link.TextContent),
                Url = TryAbsoluteUrl(source.BaseUrl, link.GetAttribute("href"))
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Title) && !string.IsNullOrWhiteSpace(item.Url))
            .Where(item => item.Title.Contains("concor", StringComparison.OrdinalIgnoreCase)
                || item.Title.Contains("selez", StringComparison.OrdinalIgnoreCase)
                || item.Title.Contains("art", StringComparison.OrdinalIgnoreCase)
                || item.Title.Contains("orchestra", StringComparison.OrdinalIgnoreCase)
                || item.Title.Contains("coro", StringComparison.OrdinalIgnoreCase))
            .DistinctBy(item => item.Url, StringComparer.OrdinalIgnoreCase)
            .Take(20);

        foreach (var link in links)
        {
            results.Add(new ScrapedBandoItem(
                Title: link.Title,
                SourceUrl: link.Url!,
                BodyText: link.Title,
                IssuerName: "inPA"));
        }

        return results;
    }

    private async Task<IEnumerable<ScrapedBandoItem>> TryScrapeApiAsync(BandoSource source, CancellationToken ct)
    {
        var apiBaseUrl = await DiscoverApiBaseUrlAsync(source.BaseUrl, ct);
        if (apiBaseUrl is null)
        {
            return [];
        }

        using var client = CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{apiBaseUrl.TrimEnd('/')}/concorsi-smart/api/concorso-public-area/search-better?page=0&size=24")
        {
            Content = new StringContent("""
                {
                  "text": "",
                  "categoriaId": null,
                  "regioneId": null,
                  "status": null,
                  "settoreId": null,
                  "provinciaCodice": null,
                  "dateFrom": null,
                  "dateTo": null,
                  "livelliAnzianitaIds": [],
                  "tipoImpiegoId": null,
                  "salaryMin": null,
                  "salaryMax": null,
                  "enteRiferimentoName": ""
                }
                """, Encoding.UTF8, "application/json")
        };

        using var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var payload = await response.Content.ReadAsStringAsync(ct);
        using var document = JsonDocument.Parse(payload);
        return ExtractItems(document.RootElement)
            .DistinctBy(item => item.SourceUrl, StringComparer.OrdinalIgnoreCase)
            .Take(25)
            .ToList();
    }

    private async Task<string?> DiscoverApiBaseUrlAsync(string pageUrl, CancellationToken ct)
    {
        using var client = CreateClient();
        var html = await client.GetStringAsync(pageUrl, ct);

        var inlineMatch = Regex.Match(html, @"apiurl[""']?\s*[:=]\s*[""'](?<url>https?://[^""']+)", RegexOptions.IgnoreCase);
        if (inlineMatch.Success)
        {
            return inlineMatch.Groups["url"].Value;
        }

        var scriptPathMatch = Regex.Match(html, @"(?<src>https?://[^""']+/dro-cerca-bandi\.js[^""']*)", RegexOptions.IgnoreCase);
        if (!scriptPathMatch.Success)
        {
            return null;
        }

        var script = await client.GetStringAsync(scriptPathMatch.Groups["src"].Value, ct);
        var scriptMatch = Regex.Match(script, @"apiurl[""']?\s*[:=]\s*[""'](?<url>https?://[^""']+)", RegexOptions.IgnoreCase);
        return scriptMatch.Success ? scriptMatch.Groups["url"].Value : null;
    }

    private IEnumerable<ScrapedBandoItem> ExtractItems(JsonElement root)
    {
        foreach (var candidate in EnumerateObjects(root))
        {
            var title = GetString(candidate, "titolo", "title", "oggetto", "profilo", "descrizione");
            var rawUrl = GetString(candidate, "url", "link", "href", "publicUrl", "slug");
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            var sourceUrl = NormalizeInpaUrl(rawUrl);
            var body = BuildBody(candidate);
            var issuer = GetString(candidate, "ente", "enteRiferimentoName", "amministrazione", "nomeEnte");
            var deadline = TryParseDate(GetString(candidate, "dataScadenza", "deadline", "terminePresentazioneDomande"));
            var publishedAt = TryParseDate(GetString(candidate, "dataPubblicazione", "publishedAt", "dataInserimento"));

            yield return new ScrapedBandoItem(
                Title: CleanText(title),
                SourceUrl: sourceUrl,
                BodyText: body,
                Deadline: deadline,
                PublishedAt: publishedAt,
                IssuerName: issuer ?? "inPA");
        }
    }

    private static IEnumerable<JsonElement> EnumerateObjects(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            yield return element;
            foreach (var property in element.EnumerateObject())
            {
                foreach (var child in EnumerateObjects(property.Value))
                {
                    yield return child;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var child in EnumerateObjects(item))
                {
                    yield return child;
                }
            }
        }
    }

    private static string? GetString(JsonElement element, params string[] names)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (!names.Any(name => string.Equals(name, property.Name, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (property.Value.ValueKind == JsonValueKind.String)
            {
                return property.Value.GetString();
            }

            if (property.Value.ValueKind == JsonValueKind.Number || property.Value.ValueKind == JsonValueKind.True || property.Value.ValueKind == JsonValueKind.False)
            {
                return property.Value.ToString();
            }
        }

        return null;
    }

    private static string BuildBody(JsonElement element)
    {
        var parts = element.EnumerateObject()
            .Where(property => property.Value.ValueKind == JsonValueKind.String)
            .Select(property => property.Value.GetString())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Take(8)
            .Select(value => CleanText(value));

        return string.Join(" | ", parts);
    }

    private static string NormalizeInpaUrl(string? rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return "https://www.inpa.gov.it/bandi-e-avvisi/";
        }

        if (rawUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return rawUrl;
        }

        return $"https://www.inpa.gov.it/{rawUrl.TrimStart('/')}";
    }

    private static DateTime? TryParseDate(string? raw)
    {
        return DateTime.TryParse(raw, out var parsed) ? parsed : null;
    }
}
