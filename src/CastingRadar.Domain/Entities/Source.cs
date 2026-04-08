using CastingRadar.Domain.Enums;

namespace CastingRadar.Domain.Entities;

public class Source
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public SourceRegion Region { get; private set; }
    public string? Url { get; private set; }
    public bool IsEnabled { get; private set; }
    public DateTime? LastScrapedAt { get; private set; }
    public int ErrorCount { get; private set; }

    private Source() { }

    public static Source Create(string name, SourceRegion region, string? url = null, bool isEnabled = true) =>
        new() { Name = name, Region = region, Url = url, IsEnabled = isEnabled, ErrorCount = 0 };

    public void SetUrl(string url) => Url = url;

    public void RecordSuccess() { LastScrapedAt = DateTime.UtcNow; ErrorCount = 0; }
    public void RecordError() => ErrorCount++;
    public void SetEnabled(bool enabled) => IsEnabled = enabled;
}
