using LinkTracker.Scrapper.Domain.Repository;

namespace LinkTracker.Scrapper.Application.Interfaces;

public interface ILinkRepository
{
    bool AddChat(long chatId);
    bool RemoveChat(long chatId);
    bool ChatExists(long chatId);

    bool AddLink(LinkDto link);
    GetLinksDto GetLinks(long chatId, int page, int pageSize, List<string>? filterTags);
    LinkDto? GetLink(long chatId, string url);
    bool UpdateLink(LinkDto link);
    bool RemoveLink(long chatId, string url);
    Task<List<LinkDto>> GetBatch(int limit, int offset);
    Task<bool> UpdateLinkAsync(LinkDto link);
    Task<int> GetCountBySource(string source);
}
