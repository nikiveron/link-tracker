using LinkTracker.Contracts.Enums;

namespace LinkTracker.Contracts;

public class UpdateEvent
{
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string Text { get; set; } = "";
    public EventType EventType { get; set; }
}
