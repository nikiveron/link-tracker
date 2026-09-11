using LinkTracker.Scrapper.Application.Interfaces;

namespace LinkTracker.Scrapper.Application.Services;

public class ChatValidationService : IChatValidationService
{
    private readonly ILogger<ChatValidationService> _logger;

    public ChatValidationService(ILogger<ChatValidationService> logger)
    {
        _logger = logger;
    }

    public bool IsValidChatId(long chatId, out string errorMessage)
    {
        if (chatId <= 0)
        {
            errorMessage = "Parameter 'id' cannot be negative or zero";
            _logger.LogDebug("ChatId validation failed: {ErrorMessage}", errorMessage);
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }
}