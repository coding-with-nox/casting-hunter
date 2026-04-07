using CastingRadar.Domain.Enums;

namespace CastingRadar.Domain.Entities;

public class Source
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public SourceRegion Region { get; private set; }
    public bool IsEnabled { get; private set; }
    public DateTime? LastScrapedAt { get; private set; }
    public int ErrorCount { get; private set; }

    private Source() { }

    public static Source Create(string name, SourceRegion region, bool isEnabled = true) =>
        new() { Name = name, Region = region, IsEnabled = isEnabled, ErrorCount = 0 };

    public void RecordSuccess() { LastScrapedAt = DateTime.UtcNow; ErrorCount = 0; }
    public void RecordError() => ErrorCount++;
    public void SetEnabled(bool enabled) => IsEnabled = enabled;
}
