namespace CastingRadar.Domain.Entities;

public class BandoSource
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public string BaseUrl { get; private set; } = string.Empty;
    public int Priority { get; private set; }
    public bool IsOfficial { get; private set; }
    public bool IsEnabled { get; private set; }
    public DateTime? LastRunAt { get; private set; }
    public int LastRunFound { get; private set; }
    public int LastRunEligible { get; private set; }
    public int LastRunNew { get; private set; }
    public string? LastRunError { get; private set; }

    private BandoSource() { }

    public static BandoSource Create(
        string name,
        string category,
        string baseUrl,
        int priority,
        bool isOfficial = true,
        bool isEnabled = true) =>
        new()
        {
            Name = name,
            Category = category,
            BaseUrl = baseUrl,
            Priority = priority,
            IsOfficial = isOfficial,
            IsEnabled = isEnabled
        };

    public void SetEnabled(bool enabled) => IsEnabled = enabled;

    public void RecordRun(int found, int eligible, int newCount, string? error = null)
    {
        LastRunAt = DateTime.UtcNow;
        LastRunFound = found;
        LastRunEligible = eligible;
        LastRunNew = newCount;
        LastRunError = error;
    }
}
