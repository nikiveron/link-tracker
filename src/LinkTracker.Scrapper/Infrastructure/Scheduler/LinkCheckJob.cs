using LinkTracker.Scrapper.Application.Interfaces;
using LinkTracker.Scrapper.Application.Services;
using LinkTracker.Scrapper.Domain.Options;
using LinkTracker.Scrapper.Domain.Repository;
using Microsoft.Extensions.Options;
using Quartz;

namespace LinkTracker.Scrapper.Infrastructure.Scheduler;

public class LinkCheckJob : IJob
{
    private readonly ILinkRepository _repository;
    private readonly IUpdateCheckerService _checker;
    private readonly INotificationSender _notificationSender;
    private readonly ScrapperOptions _options;
    private readonly ILogger<LinkCheckJob> _logger;
    private readonly IMetricsService _metricsService;

    public LinkCheckJob(
        ILinkRepository repository,
        IUpdateCheckerService checker,
        INotificationSender bot,
        IOptions<ScrapperOptions> options,
        ILogger<LinkCheckJob> logger,
        IMetricsService metricsService)
    {
        _repository = repository;
        _checker = checker;
        _notificationSender = bot;
        _options = options.Value;
        _logger = logger;
        _metricsService = metricsService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Start checking updates");

        // Обновляем метрики количества ссылок
        await UpdateMetrics();

        int offset = 0;

        while (true)
        {
            var links = await _repository.GetBatch(_options.BatchSize, offset);
            if (links.Count == 0)
            {
                break;
            }

            await CheckEveryLink(links);

            offset += _options.BatchSize;
        }
    }

    private async Task UpdateMetrics()
    {
        try
        {
            var githubCount = await _repository.GetCountBySource("github.com");
            var stackoverflowCount = await _repository.GetCountBySource("stackoverflow.com");

            _metricsService.TrackLinksOnTrack("github.com", githubCount);
            _metricsService.TrackLinksOnTrack("stackoverflow.com", stackoverflowCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating metrics");
        }
    }

    private async Task CheckEveryLink(List<LinkDto> links)
    {
        foreach (var link in links)
        {
            try
            {
                var update = await _checker.CheckUpdateAsync(link);
                if (update == null)
                {
                    continue;
                }

                _logger.LogInformation("Обновление найдено для ссылки {Url}", update.Url);

                await _notificationSender.SendAsync(update, CancellationToken.None);

                link.LastUpdatedAt = update.NewUpdatedAt;
                link.LastNotifiedAt = DateTime.UtcNow;

                await _repository.UpdateLinkAsync(link);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing link {Id}", link.Id);
            }
        }
    }
}
