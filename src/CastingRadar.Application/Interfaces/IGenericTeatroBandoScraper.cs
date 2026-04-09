using CastingRadar.Domain.Entities;

namespace CastingRadar.Application.Interfaces;

public interface IGenericTeatroBandoScraper
{
    Task<IEnumerable<ScrapedBandoItem>> ScrapeForSourceAsync(BandoSource source, CancellationToken ct = default);
}
