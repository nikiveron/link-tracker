using LinkTracker.Scrapper.Domain.DTOs.ControllersAndClients;

namespace LinkTracker.Scrapper.Application.Interfaces;

public interface ILinkRequestValidator
{
    bool ValidateChatId(long chatId, out ApiErrorResponse? error);
    bool ValidatePage(int page, out ApiErrorResponse? error);
    bool ValidatePageSize(int pageSize, out ApiErrorResponse? error);
    bool ValidateLink(string? url, out ApiErrorResponse? error);
    bool ValidateChatExists(long chatId, ILinkRepository repository, out ApiErrorResponse? error);
}