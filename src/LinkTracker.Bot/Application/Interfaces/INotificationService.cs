using LinkTracker.Contracts;

namespace LinkTracker.Bot.Application.Interfaces;

public interface INotificationService
{
    Task NotifyUsersAsync(LinkUpdateDto? update, CancellationToken ct);
}
