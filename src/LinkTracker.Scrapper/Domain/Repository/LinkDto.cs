namespace LinkTracker.Scrapper.Domain.Repository;

public class LinkDto
{
    public long Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    public DateTime? LastNotifiedAt { get; set; }
    public long ChatId { get; set; }

    public override string ToString()
    {
        return "LinkDto { " +
               $"Id: {Id}, " +
               $"Url: {Url}, " +
               $"Tags: [{string.Join(", ", Tags)}], " +
               $"CreatedAt: {CreatedAt}, " +
               $"LastUpdatedAt: {LastUpdatedAt}, " +
               $"LastNotifiedAt: {LastNotifiedAt}, " +
               $"ChatId: {ChatId} " +
               "}";
    }
}