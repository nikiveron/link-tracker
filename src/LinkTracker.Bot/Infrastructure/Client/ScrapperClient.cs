using LinkTracker.Bot.Domain.Exceptions;
using LinkTracker.Scrapper.Domain.DTOs.ControllersAndClients;
using System.Net;
using System.Text;
using System.Text.Json;

namespace LinkTracker.Bot.Infrastructure.Client;

public class ScrapperClient : IScrapperClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ScrapperClient> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ScrapperClient(HttpClient httpClient, ILogger<ScrapperClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task RegisterChatAsync(long chatId, CancellationToken ct = default)
    {
        var response = await _httpClient.PostAsync($"/tg-chat/{chatId}", null, ct);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Чат {ChatId} Зарегестрирован в Scrapper", chatId);
            return;
        }

        await HandleErrorResponse(response, "RegisterChat", ct);
    }

    public async Task AddLinkAsync(long chatId, string link, List<string> tags, CancellationToken ct)
    {
        var request = new AddLinkRequest { Uri = link, Tags = tags ?? [] };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/links")
        {
            Content = content
        };
        requestMessage.Headers.Add("Tg-Chat-Id", chatId.ToString());

        var response = await _httpClient.SendAsync(requestMessage, ct);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Ссылка {Link} добавлена для чата {ChatId}", link, chatId);
            return;
        }

        await HandleErrorResponse(response, "AddLink", ct);
    }

    public async Task RemoveLinkAsync(long chatId, string link, CancellationToken ct)
    {
        var request = new RemoveLinkRequest { Link = link };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var requestMessage = new HttpRequestMessage(HttpMethod.Delete, "/links")
        {
            Content = content
        };
        requestMessage.Headers.Add("Tg-Chat-Id", chatId.ToString());

        var response = await _httpClient.SendAsync(requestMessage, ct);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Ссылка {Link} удалена для чата {ChatId}", link, chatId);
            return;
        }

        await HandleErrorResponse(response, "RemoveLink", ct);
    }

    public async Task<ListLinksResponse?> GetLinksAsync(long chatId, int page = 1, int pageSize = 10, List<string>? filterTags = null, CancellationToken ct = default)
    {
        var url = $"/links?page={page}&pageSize={pageSize}";

        if (filterTags != null && filterTags.Count != 0)
        {
            var tagsParam = string.Join(",", filterTags.Select(t => Uri.EscapeDataString(t)));
            url += $"&tags={tagsParam}";
        }

        var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
        requestMessage.Headers.Add("Tg-Chat-Id", chatId.ToString());

        var response = await _httpClient.SendAsync(requestMessage, ct);

        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<ListLinksResponse>(json, _jsonSerializerOptions);
            return result;
        }

        await HandleErrorResponse(response, "GetLinks", ct);
        return new ListLinksResponse();
    }

    private async Task HandleErrorResponse(HttpResponseMessage response, string operation, CancellationToken ct)
    {
        var statusCode = response.StatusCode;
        var content = await response.Content.ReadAsStringAsync(ct);

        ApiErrorResponse? error = null;
        try
        {
            error = JsonSerializer.Deserialize<ApiErrorResponse>(content);
        }
        catch
        {
            // Игнорируем ошибку парсинга, если ответ не в ожидаемом формате
        }

        var errorMessage = error?.Description ?? response.ReasonPhrase ?? "Неизвестная ошибка";

        _logger.LogWarning("Scrapper API вернул {StatusCode} для {Operation}: {Error}", statusCode, operation, errorMessage);

        throw statusCode switch
        {
            HttpStatusCode.BadRequest => new ScrapperBadRequestException(errorMessage, operation, error),
            HttpStatusCode.NotFound => new ScrapperNotFoundException(errorMessage, operation, error),
            HttpStatusCode.Conflict => new ScrapperConflictException(errorMessage, operation, error),
            _ => new ScrapperApiException($"Scrapper API ошибка: {statusCode} - {errorMessage}", statusCode, operation, error),
        };
    }
}