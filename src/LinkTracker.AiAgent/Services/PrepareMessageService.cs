using LinkTracker.AiAgent.Domain;
using LinkTracker.Contracts;
using Microsoft.Extensions.Options;

namespace LinkTracker.AiAgent.Services;

public class PrepareMessageService(
    IGroupingService groupingService,
    IPriorityService priorityService,
    IOptions<AiAgentOptions> options,
    ILogger<PrepareMessageService> logger) : IPrepareMessageService
{
    private readonly AiAgentOptions _options = options.Value;

    public async Task Execute(LinkUpdateDto? update, CancellationToken cancellationToken)
    {
        if (update == null) { return; }

        logger.LogInformation("Получено обновление для ChatId={ChatId}, событий={Count}", update.ChatId, update.Events.Count);

        var filteredEvents = new List<UpdateEvent>();
        foreach (var evt in update.Events)
        {
            if (evt.Text.Length < _options.MinLength)
            {
                logger.LogDebug("Событие отфильтровано по длине (длина={Length})", evt.Text.Length);
                continue;
            }
            if (_options.Filtering.StopWords.Any(w => evt.Text.Contains(w, StringComparison.OrdinalIgnoreCase)))
            {
                logger.LogDebug("Событие отфильтровано по стоп-слову");
                continue;
            }
            if (_options.Filtering.ExcludedAuthors.Any(a => evt.Author.StartsWith(a, StringComparison.OrdinalIgnoreCase)))
            {
                logger.LogDebug("Событие отфильтровано по автору");
                continue;
            }

            if (evt.Text.Length > _options.Summarization.Threshold)
            {
                evt.Text = evt.Text.SummarizeText();
            }

            var priority = priorityService.DeterminePriority(evt);
            logger.LogDebug("Приоритет события: {Priority}", priority);

            filteredEvents.Add(evt);
        }

        if (filteredEvents.Count == 0)
        {
            logger.LogInformation("Все события отфильтрованы для ChatId={ChatId}", update.ChatId);
            return;
        }

        var filteredUpdate = new LinkUpdateDto
        {
            Id = update.Id,
            Url = update.Url,
            Description = update.Description,
            ChatId = update.ChatId,
            NewUpdatedAt = update.NewUpdatedAt,
            Events = filteredEvents
        };

        groupingService.AddUpdate(filteredUpdate, cancellationToken);

        logger.LogInformation("Обновление добавлено в группировку для ChatId={ChatId}", update.ChatId);
    }
}
