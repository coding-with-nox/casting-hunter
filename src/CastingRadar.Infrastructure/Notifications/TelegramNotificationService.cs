using CastingRadar.Application.Interfaces;
using CastingRadar.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace CastingRadar.Infrastructure.Notifications;

public class TelegramNotificationService(
    IConfiguration configuration,
    ILogger<TelegramNotificationService> logger) : INotificationService
{
    private ITelegramBotClient? _botClient;
    private string? _chatId;

    private bool TryInitialize()
    {
        var token = configuration["CastingRadar:Telegram:BotToken"];
        _chatId = configuration["CastingRadar:Telegram:ChatId"];

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(_chatId))
        {
            logger.LogDebug("Telegram not configured — notifications skipped");
            return false;
        }

        _botClient ??= new TelegramBotClient(token);
        return true;
    }

    public async Task SendAsync(CastingCall castingCall, CancellationToken cancellationToken = default)
    {
        if (!TryInitialize()) return;

        var isPaidEmoji = castingCall.IsPaid ? "✅ Sì" : "❌ No";
        var deadline = castingCall.Deadline.HasValue
            ? castingCall.Deadline.Value.ToString("d MMMM yyyy")
            : "Non specificata";

        // Validate SourceUrl before embedding it (prevents Telegram Markdown injection)
        var safeUrl = IsValidHttpUrl(castingCall.SourceUrl) ? castingCall.SourceUrl : "#";

        var text = $"""
            🎬 *Nuovo Casting: {EscapeMarkdown(castingCall.Title)}*
            📍 Luogo: {EscapeMarkdown(castingCall.Location ?? "Non specificato")}
            🎭 Tipo: {castingCall.Type}
            💰 Retribuito: {isPaidEmoji}
            ⏰ Scadenza: {deadline}
            🔗 [Candidati ora]({safeUrl})
            """;

        try
        {
            await _botClient!.SendMessage(
                chatId: _chatId!,
                text: text,
                parseMode: ParseMode.Markdown,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send Telegram message for casting {Id}", castingCall.Id);
            throw;
        }
    }

    private static string EscapeMarkdown(string text) =>
        text.Replace("*", "\\*").Replace("_", "\\_").Replace("[", "\\[").Replace("`", "\\`");

    private static bool IsValidHttpUrl(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp)
        && !uri.IsLoopback
        && !IsPrivateIp(uri.Host);

    private static bool IsPrivateIp(string host)
    {
        // Block RFC-1918 / link-local ranges to prevent SSRF via Telegram links
        if (System.Net.IPAddress.TryParse(host, out var ip))
        {
            var bytes = ip.GetAddressBytes();
            return bytes[0] == 10
                || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                || (bytes[0] == 192 && bytes[1] == 168)
                || (bytes[0] == 169 && bytes[1] == 254); // link-local
        }
        return false;
    }
}
