using CastingRadar.Domain.Entities;
using CastingRadar.Domain.Enums;
using CastingRadar.Domain.ValueObjects;

namespace CastingRadar.Infrastructure.Tests.Scrapers;

public class ContentHashTests
{
    [Fact]
    public void ComputeHash_ReturnsSameValueForIdenticalInput()
    {
        var h1 = CastingCall.ComputeHash("Provino Film Roma", "https://ticonsiglio.com/casting/1");
        var h2 = CastingCall.ComputeHash("Provino Film Roma", "https://ticonsiglio.com/casting/1");
        Assert.Equal(h1, h2);
    }

    [Fact]
    public void ComputeHash_IsHexString()
    {
        var hash = CastingCall.ComputeHash("Title", "https://example.com");
        Assert.Matches("^[0-9a-f]{64}$", hash);
    }
}

public class ScraperFilterTests
{
    [Fact]
    public void Default_HasItalyRegion()
    {
        var filter = ScraperFilter.Default;
        Assert.Contains(SourceRegion.Italy, filter.Regions!);
    }

    [Fact]
    public void Default_GenderIsFemale()
    {
        Assert.Equal("female", ScraperFilter.Default.GenderFilter);
    }

    [Fact]
    public void WithRecord_CanOverrideProperties()
    {
        var filter = ScraperFilter.Default with { OnlyPaid = true };
        Assert.True(filter.OnlyPaid);
        Assert.Contains(SourceRegion.Italy, filter.Regions!);
    }
}
