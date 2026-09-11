using LinkTracker.Contracts;

namespace LinkTracker.AiAgent.Domain;

public class ProcessedUpdate
{
    public long ChatId { get; set; }
    public Priority Priority { get; set; }
    public List<UpdateEvent> Events { get; set; } = [];
    public DateTime WindowStart { get; set; }
}
