using LinkTracker.Contracts;
using LinkTracker.Scrapper.Domain.Repository;

namespace LinkTracker.Scrapper.Application.Interfaces;

public interface IUpdateCheckerService
{
    Task<LinkUpdateDto?> CheckUpdateAsync(LinkDto link);
}
