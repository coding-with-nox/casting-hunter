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

        var text = $"""
            🎬 *Nuovo Casting: {EscapeMarkdown(castingCall.Title)}*
            📍 Luogo: {EscapeMarkdown(castingCall.Location ?? "Non specificato")}
            🎭 Tipo: {castingCall.Type}
            💰 Retribuito: {isPaidEmoji}
            ⏰ Scadenza: {deadline}
            🔗 [Candidati ora]({castingCall.SourceUrl})
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
}
