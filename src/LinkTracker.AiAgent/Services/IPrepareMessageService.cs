using LinkTracker.Contracts;

namespace LinkTracker.AiAgent.Services;

public interface IPrepareMessageService
{
    Task Execute(LinkUpdateDto? update, CancellationToken cancellationToken);
}
