using LinkTracker.Scrapper.Domain.DTOs.ControllersAndClients;
using LinkTracker.Scrapper.Domain.Repository;

namespace LinkTracker.Scrapper.Application.Services;

public class LinksControllerHelperService
{
    public List<string>? ParseFilterTags(string? tags)
    {
        return string.IsNullOrWhiteSpace(tags)
            ? null
            : [.. tags.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim().ToLowerInvariant())];
    }

    public ListLinksResponse BuildLinksResponse(GetLinksDto linksDto, int page, int pageSize)
    {
        var links = linksDto.PagedLinks;
        var totalPages = (int)Math.Ceiling(linksDto.TotalCount / (double)pageSize);

        return new ListLinksResponse
        {
            Links = [.. links.Select(l => new LinkResponse
            {
                Id = l.Id,
                Url = l.Url,
                Tags = l.Tags
            })],
            Size = links.Count,
            Page = page,
            PageSize = pageSize,
            TotalCount = linksDto.TotalCount,
            TotalPages = totalPages
        };
    }

    public void LogGetLinksResult(ListLinksResponse response)
    {
        var uniqueTags = response.Links
            .SelectMany(l => l.Tags)
            .Distinct()
            .Count();
    }
}
