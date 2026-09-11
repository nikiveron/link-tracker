using LinkTracker.Scrapper.Domain.DTOs.ControllersAndClients;

namespace LinkTracker.Bot.Infrastructure.Client;

public interface IScrapperClient
{
    Task RegisterChatAsync(long chatId, CancellationToken ct = default);
    Task AddLinkAsync(long chatId, string link, List<string> tags, CancellationToken cancellationToken);
    Task RemoveLinkAsync(long chatId, string link, CancellationToken cancellationToken);
    Task<ListLinksResponse?> GetLinksAsync(long chatId, int page = 0, int pageSize = 10, List<string>? filterTags = null, CancellationToken cancellationToken = default);
}
