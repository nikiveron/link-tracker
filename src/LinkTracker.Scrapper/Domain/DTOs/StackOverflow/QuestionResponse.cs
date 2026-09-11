namespace LinkTracker.Scrapper.Domain.DTOs.StackOverflow;

public class QuestionResponse
{
    public long QuestionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public long CreationDate { get; set; }
    public long LastActivityDate { get; set; }
    public OwnerResponse Owner { get; set; } = new();
}