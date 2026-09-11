using LinkTracker.AiAgent.Domain;
using LinkTracker.Contracts;
using Microsoft.Extensions.Options;

namespace LinkTracker.AiAgent.Services;

public interface IPriorityService
{
    Priority DeterminePriority(UpdateEvent evt);
    Priority GetMaxPriority(IEnumerable<Priority> priorities);
}

public class PriorityService : IPriorityService
{
    private readonly PrioritizationOptions _options;
    private readonly ILogger<PriorityService> _logger;

    public PriorityService(IOptions<AiAgentOptions> options, ILogger<PriorityService> logger)
    {
        _options = options.Value.Prioritization;
        _logger = logger;
    }

    public Priority DeterminePriority(UpdateEvent evt)
    {
        var text = evt.Text.ToLowerInvariant();

        if (_options.HighKeywords.Any(kw => text.Contains(kw.ToLowerInvariant())))
        {
            _logger.LogDebug("Обнаружен HIGH приоритет для события: {Title}", evt.Title);
            return Priority.HIGH;
        }

        if (_options.LowKeywords.Any(kw => text.Contains(kw.ToLowerInvariant())))
        {
            _logger.LogDebug("Обнаружен LOW приоритет для события: {Title}", evt.Title);
            return Priority.LOW;
        }

        _logger.LogDebug("Приоритет MEDIUM для события: {Title}", evt.Title);
        return Priority.MEDIUM;
    }

    public Priority GetMaxPriority(IEnumerable<Priority> priorities)
    {
        if (!priorities.Any())
        {
            return Priority.MEDIUM;
        }

        return priorities.Max();
    }
}
