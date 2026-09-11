namespace LinkTracker.Scrapper.Application.Interfaces;

public interface IChatValidationService
{
    bool IsValidChatId(long chatId, out string errorMessage);
}
