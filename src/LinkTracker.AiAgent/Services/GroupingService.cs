using LinkTracker.AiAgent.Domain;
using LinkTracker.Contracts;
using Microsoft.Extensions.Options;

namespace LinkTracker.AiAgent.Services;

public interface IGroupingService
{
    void AddUpdate(LinkUpdateDto update, CancellationToken cancellationToken);
    Task FlushReadyMessages(CancellationToken cancellationToken);
}

public class GroupingService : IGroupingService
{
    private readonly GroupingOptions _options;
    private readonly IPriorityService _priorityService;
    private readonly KafkaPublisher _publisher;
    private readonly ILogger<GroupingService> _logger;

    private readonly Dictionary<long, ProcessedUpdate> _groupedUpdates = [];

    public GroupingService(
        IOptions<AiAgentOptions> options,
        IPriorityService priorityService,
        KafkaPublisher publisher,
        ILogger<GroupingService> logger)
    {
        _options = options.Value.Grouping;
        _priorityService = priorityService;
        _publisher = publisher;
        _logger = logger;
    }

    public void AddUpdate(LinkUpdateDto update, CancellationToken cancellationToken)
    {
        var windowStart = DateTime.UtcNow;

        if (!_groupedUpdates.TryGetValue(update.ChatId, out var grouped))
        {
            grouped = new ProcessedUpdate
            {
                ChatId = update.ChatId,
                WindowStart = windowStart,
                Events = [],
                Priority = Priority.MEDIUM
            };
            _groupedUpdates[update.ChatId] = grouped;
        }

        foreach (var evt in update.Events)
        {
            grouped.Events.Add(evt);
        }

        var priorities = grouped.Events.Select(e => _priorityService.DeterminePriority(e));
        grouped.Priority = _priorityService.GetMaxPriority(priorities);


        _logger.LogInformation("Добавлено обновление для ChatId={ChatId}, всего событий в группе: {Count}",
            update.ChatId, grouped.Events.Count);
    }

    public async Task FlushReadyMessages(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        List<ProcessedUpdate> toFlush = [];

        var readyKeys = _groupedUpdates
            .Where(kvp => (now - kvp.Value.WindowStart).TotalMilliseconds >= _options.WindowMs)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in readyKeys)
        {
            if (_groupedUpdates.TryGetValue(key, out var update))
            {
                toFlush.Add(update);
                _groupedUpdates.Remove(key);
            }
        }


        foreach (var update in toFlush)
        {
            await FlushUpdate(update, cancellationToken);
        }
    }

    private async Task FlushUpdate(ProcessedUpdate update, CancellationToken cancellationToken)
    {
        if (update.Events.Count == 0)
        {
            _logger.LogWarning("Попытка отправки пустого обновления для ChatId={ChatId}", update.ChatId);
            return;
        }

        var linkUpdate = new LinkUpdateDto
        {
            ChatId = update.ChatId,
            Id = update.Events.FirstOrDefault()?.Url?.GetHashCode() ?? 0,
            Url = update.Events.FirstOrDefault()?.Url ?? string.Empty,
            Description = FormatDescription(update.Events),
            NewUpdatedAt = DateTime.UtcNow,
            Events = update.Events
        };

        linkUpdate.Description = $"[{update.Priority}] {linkUpdate.Description}";

        await _publisher.Produce(linkUpdate, cancellationToken);

        _logger.LogInformation("Отправлено сгруппированное сообщение для ChatId={ChatId}, приоритет={Priority}, событий={Count}",
            update.ChatId, update.Priority, update.Events.Count);
    }

    private static string FormatDescription(List<UpdateEvent> events)
    {
        if (events.Count == 1)
        {
            return events[0].Text;
        }

        var descriptions = events
            .Select((e, i) => $"{i + 1}. {e.Text}")
            .ToList();

        return string.Join("\n", descriptions);
    }
}
