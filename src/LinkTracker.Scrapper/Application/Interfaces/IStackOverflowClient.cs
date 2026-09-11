using LinkTracker.Contracts;

namespace LinkTracker.Scrapper.Application.Interfaces;

public interface IStackOverflowClient
{
    Task<List<UpdateEvent>> GetUpdates(long questionId, DateTime? since);
}
