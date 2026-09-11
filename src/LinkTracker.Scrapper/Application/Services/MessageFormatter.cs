using LinkTracker.Contracts;
using LinkTracker.Scrapper.Application.Interfaces;

namespace LinkTracker.Scrapper.Application.Services;

public class MessageFormatter : IMessageFormatter
{
    public string Format(LinkUpdateDto update)
    {
        return $"🔔 Обновление:\n{update.Url}\n{update.Description}";
    }
}
