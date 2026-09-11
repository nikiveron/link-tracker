using LinkTracker.Contracts;

namespace LinkTracker.Scrapper.Application.Interfaces;

public interface IMessageFormatter
{
    string Format(LinkUpdateDto update);
}
