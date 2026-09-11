using LinkTracker.Scrapper.Application.Interfaces;
using LinkTracker.Scrapper.Domain.Entities;
using LinkTracker.Scrapper.Domain.Repository;
using LinkTracker.Scrapper.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

public class OrmLinkRepository : ILinkRepository
{
    private readonly ChatsContext _context;

    public OrmLinkRepository(ChatsContext context)
    {
        _context = context;
    }

    public bool AddChat(long chatId)
    {
        if (_context.Chats.Any(c => c.Id == chatId))
        {
            return false;
        }

        _context.Chats.Add(new Chat { Id = chatId });
        _context.SaveChanges();
        return true;
    }

    public bool ChatExists(long chatId) => _context.Chats.Any(c => c.Id == chatId);

    public bool RemoveChat(long chatId)
    {
        var chat = _context.Chats.Find(chatId);
        if (chat == null)
        {
            return false;
        }

        _context.Chats.Remove(chat);
        _context.SaveChanges();
        return true;
    }

    public bool AddLink(LinkDto linkDto)
    {
        if (!_context.Chats.Any(c => c.Id == linkDto.ChatId))
        {
            return false;
        }

        if (_context.Links.Any(l => l.ChatId == linkDto.ChatId && l.Url == linkDto.Url))
        {
            return false;
        }

        var link = new Link
        {
            Url = linkDto.Url,
            ChatId = linkDto.ChatId,
            Tags = [.. linkDto.Tags.Select(t =>
                _context.Tags.FirstOrDefault(x => x.Name == t)
                ?? new Tag { Name = t })],
            CreatedAt = DateTime.UtcNow
        };

        _context.Links.Add(link);
        _context.SaveChanges();
        return true;
    }

    public GetLinksDto GetLinks(long chatId, int page, int pageSize, List<string>? filterTags)
    {
        var query = _context.Links
            .Include(l => l.Tags)
            .Where(l => l.ChatId == chatId);

        if (filterTags != null && filterTags.Count != 0)
        {
            var normalizedFilterTags = filterTags.Select(t => t.ToLowerInvariant()).ToList();
            query = query.Where(l => normalizedFilterTags.All(tag => l.Tags.Any(t => t.Name.ToLower() == tag)));
        }

        int totalCount = query.Count();

        var pagedLinks = query
            .OrderBy(l => l.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LinkDto
            {
                Id = l.Id,
                Url = l.Url,
                Tags = l.Tags.Select(t => t.Name).ToList(),
                CreatedAt = l.CreatedAt,
                LastUpdatedAt = l.LastUpdatedAt,
                LastNotifiedAt = l.LastNotifiedAt
            })
            .ToList();

        return new GetLinksDto
        {
            PagedLinks = pagedLinks,
            TotalCount = totalCount
        };
    }

    public LinkDto? GetLink(long chatId, string url)
    {
        var link = _context.Links.Include(l => l.Tags).FirstOrDefault(l => l.ChatId == chatId && l.Url == url);

        return link == null ? null
            : new LinkDto
            {
                Id = link.Id,
                Url = link.Url,
                Tags = [.. link.Tags.Select(t => t.Name)],
                CreatedAt = link.CreatedAt,
                LastUpdatedAt = link.LastUpdatedAt,
                LastNotifiedAt = link.LastNotifiedAt,
                ChatId = link.ChatId
            };
    }

    public bool UpdateLink(LinkDto linkDto)
    {
        var link = _context.Links.FirstOrDefault(l => l.ChatId == linkDto.ChatId && l.Url == linkDto.Url);

        if (link == null)
        {
            return false;
        }

        if (linkDto.LastNotifiedAt != null)
        {
            link.LastNotifiedAt = linkDto.LastNotifiedAt;
        }

        if (linkDto.LastUpdatedAt != null)
        {
            link.LastUpdatedAt = linkDto.LastUpdatedAt;
        }

        _context.Links.Update(link);
        _context.SaveChanges();

        return true;
    }

    public bool RemoveLink(long chatId, string url)
    {
        var link = _context.Links.FirstOrDefault(l => l.ChatId == chatId && l.Url == url);

        if (link == null)
        {
            return false;
        }

        _context.Links.Remove(link);
        _context.SaveChanges();

        return true;
    }

    public async Task<List<LinkDto>> GetBatch(int limit, int offset)
    {
        return await _context.Links
            .Include(l => l.Tags)
            .OrderBy(l => l.Id)
            .Skip(offset)
            .Take(limit)
            .Select(l => new LinkDto { Id = l.Id, Url = l.Url, Tags = l.Tags.Select(t => t.Name).ToList(), CreatedAt = l.CreatedAt, LastNotifiedAt = l.LastNotifiedAt, LastUpdatedAt = l.LastUpdatedAt, ChatId = l.ChatId })
            .ToListAsync();
    }

    public async Task<bool> UpdateLinkAsync(LinkDto linkDto)
    {
        var link = _context.Links.FirstOrDefault(l => l.ChatId == linkDto.ChatId && l.Url == linkDto.Url);

        if (link == null)
        {
            return false;
        }

        if (linkDto.LastNotifiedAt != null)
        {
            link.LastNotifiedAt = linkDto.LastNotifiedAt;
        }

        if (linkDto.LastUpdatedAt != null)
        {
            link.LastUpdatedAt = linkDto.LastUpdatedAt;
        }

        _context.Links.Update(link);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<int> GetCountBySource(string source)
    {
        return await _context.Links
            .Where(l => l.Url.Contains(source))
            .CountAsync();
    }
}
