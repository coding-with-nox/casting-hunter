namespace CastingRadar.Domain.Entities;

public class BandoKeywordExclusion
{
    public int Id { get; private set; }
    public string Word { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private BandoKeywordExclusion() { }

    public static BandoKeywordExclusion Create(string word) =>
        new() { Word = word.Trim().ToLowerInvariant(), CreatedAt = DateTime.UtcNow };
}
