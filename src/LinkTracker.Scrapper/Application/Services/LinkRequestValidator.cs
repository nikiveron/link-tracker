using LinkTracker.Scrapper.Application.Interfaces;
using LinkTracker.Scrapper.Domain.DTOs.ControllersAndClients;

namespace LinkTracker.Scrapper.Application.Services;

public class LinkRequestValidator : ILinkRequestValidator
{
    private readonly ILogger<LinkRequestValidator> _logger;

    public LinkRequestValidator(ILogger<LinkRequestValidator> logger)
    {
        _logger = logger;
    }

    public bool ValidateChatId(long chatId, out ApiErrorResponse? error)
    {
        if (chatId <= 0)
        {
            error = ApiErrorFactory.CreateBadRequest("Некорректные параметры запроса", "INVALID_CHAT_ID", "Parameter 'id' cannot be negative or zero");
            _logger.LogDebug("Validation failed: Invalid ChatId {ChatId}", chatId);
            return false;
        }

        error = null;
        return true;
    }

    public bool ValidatePage(int page, out ApiErrorResponse? error)
    {
        if (page < 1)
        {
            error = ApiErrorFactory.CreateBadRequest("Некорректные параметры запроса", "INVALID_PAGE", "Page must be greater than or equal to 1");
            _logger.LogDebug("Validation failed: Invalid page {Page}", page);
            return false;
        }

        error = null;
        return true;
    }

    public bool ValidatePageSize(int pageSize, out ApiErrorResponse? error)
    {
        if (pageSize < 1 || pageSize > 20)
        {
            error = ApiErrorFactory.CreateBadRequest("Некорректные параметры запроса", "INVALID_PAGE_SIZE", "Page size must be between 1 and 20");
            _logger.LogDebug("Validation failed: Invalid page size {PageSize}", pageSize);
            return false;
        }

        error = null;
        return true;
    }

    public bool ValidateLink(string? url, out ApiErrorResponse? error)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            error = ApiErrorFactory.CreateBadRequest("Некорректные параметры запроса", "INVALID_LINK", "Parameter 'uri' is not a valid URL");
            _logger.LogDebug("Validation failed: Invalid URL {Url}", url);
            return false;
        }

        error = null;
        return true;
    }

    public bool ValidateChatExists(long chatId, ILinkRepository repository, out ApiErrorResponse? error)
    {
        if (!repository.ChatExists(chatId))
        {
            error = ApiErrorFactory.CreateBadRequest("Чат не найден", "CHAT_NOT_FOUND", "Chat not found");
            _logger.LogDebug("Validation failed: Chat {ChatId} not found", chatId);
            return false;
        }

        error = null;
        return true;
    }
}