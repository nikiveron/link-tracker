using LinkTracker.Contracts;
using LinkTracker.Scrapper.Application.Interfaces;

namespace LinkTracker.Scrapper.Infrastructure.Clients;

public class BotClientNotificationSender : INotificationSender
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BotClientNotificationSender> _logger;

    public BotClientNotificationSender(HttpClient httpClient, ILogger<BotClientNotificationSender> logger)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("http://bot:80");
        _logger = logger;
    }

    public async Task SendAsync(LinkUpdateDto update, CancellationToken ct)
    {
        _logger.LogInformation("Отправляем обновление в Bot для {Url} для чата {chatId}", update.Url, update.ChatId);

        var response = await _httpClient.PostAsJsonAsync("/updates", update, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);

            _logger.LogError(
                "Bot вернул ошибку {StatusCode}. Body: {Body}",
                response.StatusCode,
                body);

            response.EnsureSuccessStatusCode();
        }
    }
}