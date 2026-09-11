using LinkTracker.Contracts;

namespace LinkTracker.Scrapper.Application.Interfaces;

public interface INotificationSender
{
    Task SendAsync(LinkUpdateDto update, CancellationToken cancellationToken);
}
