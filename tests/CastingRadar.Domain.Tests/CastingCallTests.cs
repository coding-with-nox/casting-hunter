using CastingRadar.Domain.Entities;
using CastingRadar.Domain.Enums;

namespace CastingRadar.Domain.Tests;

public class CastingCallTests
{
    [Fact]
    public void ContentHash_IsDeterministic_ForSameTitleAndUrl()
    {
        var hash1 = CastingCall.ComputeHash("Provino Film Roma", "https://example.com/provino-1");
        var hash2 = CastingCall.ComputeHash("Provino Film Roma", "https://example.com/provino-1");
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ContentHash_Differs_ForDifferentUrl()
    {
        var hash1 = CastingCall.ComputeHash("Provino Film", "https://example.com/1");
        var hash2 = CastingCall.ComputeHash("Provino Film", "https://example.com/2");
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ContentHash_IsCaseInsensitive()
    {
        var hash1 = CastingCall.ComputeHash("Provino Film", "https://example.com/1");
        var hash2 = CastingCall.ComputeHash("PROVINO FILM", "HTTPS://EXAMPLE.COM/1");
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Create_SetsIsFavoriteAndIsAppliedToFalse()
    {
        var call = CastingCall.Create("Title", "Desc", "https://example.com", "Source", CastingType.Film, SourceRegion.Italy);
        Assert.False(call.IsFavorite);
        Assert.False(call.IsApplied);
    }

    [Fact]
    public void ToggleFavorite_FlipsValue()
    {
        var call = CastingCall.Create("Title", "Desc", "https://example.com", "Source", CastingType.Film, SourceRegion.Italy);
        call.ToggleFavorite();
        Assert.True(call.IsFavorite);
        call.ToggleFavorite();
        Assert.False(call.IsFavorite);
    }

    [Fact]
    public void MarkAsApplied_SetsIsAppliedToTrue()
    {
        var call = CastingCall.Create("Title", "Desc", "https://example.com", "Source", CastingType.Film, SourceRegion.Italy);
        call.MarkAsApplied();
        Assert.True(call.IsApplied);
    }
}
