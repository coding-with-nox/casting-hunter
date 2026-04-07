using CastingRadar.Application.Interfaces;
using CastingRadar.Application.UseCases.GetCastingCalls;
using CastingRadar.Application.UseCases.MarkAsApplied;
using CastingRadar.Application.UseCases.MarkAsFavorite;
using CastingRadar.Application.UseCases.ScrapeAllSources;
using CastingRadar.Application.UseCases.UpdateUserProfile;
using CastingRadar.Infrastructure.Http;
using CastingRadar.Infrastructure.Notifications;
using CastingRadar.Infrastructure.Persistence;
using CastingRadar.Infrastructure.Persistence.Repositories;
using CastingRadar.Infrastructure.Scrapers.InternationalSources;
using CastingRadar.Infrastructure.Scrapers.ItalianSources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CastingRadar.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool usePostgres = false)
    {
        // Database
        if (usePostgres)
        {
            services.AddDbContext<CastingRadarDbContext>(opt =>
                opt.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
        }
        else
        {
            services.AddDbContext<CastingRadarDbContext>(opt =>
                opt.UseSqlite(configuration.GetConnectionString("DefaultConnection") ?? "Data Source=castingradar.db"));
        }

        // Repositories
        services.AddScoped<ICastingRepository, CastingRepository>();
        services.AddScoped<ISourceRepository, SourceRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();

        // Notifications
        services.AddScoped<INotificationService, TelegramNotificationService>();

        // HTTP clients
        services.AddScraperHttpClient("Scraper");

        // Scrapers (Italian)
        services.AddScoped<ICastingScraperStrategy, TiconsiglioScraper>();
        services.AddScoped<ICastingScraperStrategy, AttoriCastingScraper>();
        services.AddScoped<ICastingScraperStrategy, IMoviezScraper>();
        services.AddScoped<ICastingScraperStrategy, ICastingScraper>();
        services.AddScoped<ICastingScraperStrategy, CastingEProviniScraper>();

        // Scrapers (International)
        services.AddScoped<ICastingScraperStrategy, MandyScraper>();
        services.AddScoped<ICastingScraperStrategy, BackstageScraper>();

        // Use cases
        services.AddScoped<ScrapeAllSourcesHandler>();
        services.AddScoped<GetCastingCallsHandler>();
        services.AddScoped<MarkAsFavoriteHandler>();
        services.AddScoped<MarkAsAppliedHandler>();
        services.AddScoped<UpdateUserProfileHandler>();

        return services;
    }

    public static async Task MigrateAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CastingRadarDbContext>();
        await db.Database.MigrateAsync();
    }
}
