namespace CastingRadar.Domain.Entities;

public class TeatroContact
{
    public int Id { get; private set; }
    /// <summary>Corrisponde a BandoSource.Name</summary>
    public string TeatroName { get; private set; } = string.Empty;
    public string? Regione { get; private set; }
    public string? Website { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Address { get; private set; }
    public string? ContactPageUrl { get; private set; }
    /// <summary>Note libere o segnalazioni dallo scraper</summary>
    public string? Notes { get; private set; }
    public DateTime? ScrapedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private TeatroContact() { }

    public static TeatroContact Create(
        string teatroName,
        string? regione,
        string? website,
        string? email = null,
        string? phone = null,
        string? address = null,
        string? contactPageUrl = null,
        string? notes = null) =>
        new()
        {
            TeatroName = teatroName.Trim(),
            Regione = regione?.Trim(),
            Website = website?.Trim(),
            Email = email?.Trim().ToLowerInvariant(),
            Phone = phone?.Trim(),
            Address = address?.Trim(),
            ContactPageUrl = contactPageUrl?.Trim(),
            Notes = notes?.Trim(),
            ScrapedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

    public void UpdateScrapeResult(
        string? email,
        string? phone,
        string? address,
        string? contactPageUrl,
        string? notes)
    {
        Email = email?.Trim().ToLowerInvariant() ?? Email;
        Phone = phone?.Trim() ?? Phone;
        Address = address?.Trim() ?? Address;
        ContactPageUrl = contactPageUrl?.Trim() ?? ContactPageUrl;
        Notes = notes?.Trim();
        ScrapedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ManualUpdate(
        string? website,
        string? email,
        string? phone,
        string? address,
        string? notes)
    {
        if (website is not null) Website = website.Trim();
        if (email is not null) Email = email.Trim().ToLowerInvariant();
        if (phone is not null) Phone = phone.Trim();
        if (address is not null) Address = address.Trim();
        if (notes is not null) Notes = notes.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
