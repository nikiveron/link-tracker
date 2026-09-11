namespace LinkTracker.Scrapper.Domain.Repository;

public class GetLinksDto
{
    public List<LinkDto> PagedLinks { get; set; } = [];
    public int TotalCount { get; set; }
}
