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
}
