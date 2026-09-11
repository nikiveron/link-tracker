using LinkTracker.Contracts;

namespace LinkTracker.Scrapper.Application.Interfaces;

public interface IGitHubClient
{
    Task<List<UpdateEvent>> GetUpdates(string owner, string repo, DateTime? since);
}