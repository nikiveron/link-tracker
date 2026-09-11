using LinkTracker.Bot.Application.Interfaces;
using LinkTracker.Contracts;
using LinkTracker.Contracts.Enums;
using System.Text;
using Telegram.Bot;

namespace LinkTracker.Bot.Application.Services;

public class NotificationService : INotificationService
{
    private readonly ITelegramBotClient _bot;
    private readonly ILogger<NotificationService> _logger;
    private readonly IBotMetricsService _metricsService;

    public NotificationService(ITelegramBotClient botClient, ILogger<NotificationService> logger, IBotMetricsService metricsService)
    {
        _bot = botClient;
        _logger = logger;
        _metricsService = metricsService;
    }

    public async Task NotifyUsersAsync(LinkUpdateDto? linkUpdate, CancellationToken ct)
    {
        if (linkUpdate == null)
        {
            return;
        }

        try
        {
            var events = linkUpdate.Events?.OrderByDescending(e => e.CreatedAt).ToList() ?? new();
            int total = events.Count;
            int take = Math.Min(5, total);

            var sb = new StringBuilder();

            sb.AppendLine("🔔 Обновление на отслеживаемой странице");
            sb.AppendLine();
            sb.AppendLine($"Ссылка: {linkUpdate.Url}");
            sb.AppendLine($"Описание: 🔔 Новые события (показаны первые {take} из {total}):");
            sb.AppendLine();

            foreach (var e in events.Take(take))
            {
                sb.AppendLine(FormatEvent(e));
            }

            var message = sb.ToString();

            await _bot.SendMessage(linkUpdate.ChatId, message, cancellationToken: ct);

            _metricsService.TrackSentNotification();
            _logger.LogDebug("Уведомление отправлено пользователю {ChatId}", linkUpdate.ChatId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка отправки уведомления пользователю {ChatId}", linkUpdate.ChatId);
        }

    }

    private static string GetEventIcon(EventType type) => type switch
    {
        EventType.PR => "🔀 PR",
        EventType.Issue => "🐛 Issue",
        EventType.Comment => "💬 Comment",
        EventType.Answer => "💡 Answer",
        _ => "🔔 Update"
    };

    private static string FormatEvent(UpdateEvent e)
    {
        var time = e.CreatedAt.ToString("yyyy-MM-dd HH:mm");

        return
        $"""
        {GetEventIcon(e.EventType)}: {e.Title}
        👤 {e.Author}
        🕐 {time}
        📝 {e.Text}
        
        """;
    }

}