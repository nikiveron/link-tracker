using LinkTracker.Contracts;
using LinkTracker.Scrapper.Application.Interfaces;
using Polly;

namespace LinkTracker.Scrapper.Infrastructure.Clients;

public class FallbackNotificationSender : INotificationSender
{
    private readonly INotificationSender _httpSender;
    private readonly INotificationSender _kafkaSender;

    public FallbackNotificationSender(
        INotificationSender httpSender,
        INotificationSender kafkaSender)
    {
        _httpSender = httpSender;
        _kafkaSender = kafkaSender;
    }

    public async Task SendAsync(LinkUpdateDto update, CancellationToken ct)
    {
        try
        {
            await _httpSender.SendAsync(update, ct);
        }
        catch
        {
            await _kafkaSender.SendAsync(update, ct);
        }
    }
}

