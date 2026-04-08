using CastingRadar.Domain.Entities;
using CastingRadar.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CastingRadar.Infrastructure.Persistence;

public class CastingRadarDbContext(DbContextOptions<CastingRadarDbContext> options) : DbContext(options)
{
    public DbSet<Bando> Bandi => Set<Bando>();
    public DbSet<BandoSource> BandoSources => Set<BandoSource>();
    public DbSet<CastingCall> CastingCalls => Set<CastingCall>();
    public DbSet<Source> Sources => Set<Source>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Bando>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ContentHash).IsUnique();
            e.Property(x => x.ConfidenceScore).HasPrecision(5, 2);
        });

        modelBuilder.Entity<BandoSource>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<CastingCall>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ContentHash).IsUnique();
            e.Property(x => x.Type).HasConversion<string>();
            e.Property(x => x.Region).HasConversion<string>();
        });

        modelBuilder.Entity<Source>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Region).HasConversion<string>();
        });

        modelBuilder.Entity<UserProfile>(e =>
        {
            e.HasKey(x => x.Id);
            var typesComparer = new ValueComparer<CastingType[]>(
                (a, b) => a != null && b != null && a.SequenceEqual(b),
                v => v.Aggregate(0, (h, t) => HashCode.Combine(h, t.GetHashCode())),
                v => v.ToArray());

            var regionsComparer = new ValueComparer<SourceRegion[]>(
                (a, b) => a != null && b != null && a.SequenceEqual(b),
                v => v.Aggregate(0, (h, r) => HashCode.Combine(h, r.GetHashCode())),
                v => v.ToArray());

            e.Property(x => x.PreferredTypes)
                .HasConversion(
                    v => string.Join(',', v.Select(t => t.ToString())),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                          .Select(Enum.Parse<CastingType>).ToArray(),
                    typesComparer);
            e.Property(x => x.PreferredRegions)
                .HasConversion(
                    v => string.Join(',', v.Select(r => r.ToString())),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                          .Select(Enum.Parse<SourceRegion>).ToArray(),
                    regionsComparer);
        });
    }
}
