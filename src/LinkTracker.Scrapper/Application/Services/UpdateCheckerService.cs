using LinkTracker.Scrapper.Application.Interfaces;
using LinkTracker.Scrapper.Domain.Repository;
using LinkTracker.Contracts;

namespace LinkTracker.Scrapper.Application.Services;

public class UpdateCheckerService : IUpdateCheckerService
{
    private readonly IGitHubClient _github;
    private readonly IStackOverflowClient _stack;
    private readonly ILogger _logger;
    private readonly IMetricsService _metricsService;

    public UpdateCheckerService(
        IGitHubClient github,
        IStackOverflowClient stack,
        ILogger<UpdateCheckerService> logger,
        IMetricsService metricsService)
    {
        _github = github;
        _stack = stack;
        _logger = logger;
        _metricsService = metricsService;
    }

    public async Task<LinkUpdateDto?> CheckUpdateAsync(LinkDto link)
    {
        if (link.Url.Contains("github.com"))
        {
            return await CheckGitHub(link);
        }

        if (link.Url.Contains("stackoverflow.com"))
        {
            return await CheckStackOverflow(link);
        }

        return null;
    }

    private async Task<LinkUpdateDto?> CheckGitHub(LinkDto link)
    {
        var (owner, repo) = ParseGitHub(link.Url);
        if (owner is null || repo is null)
        {
            return null;
        }

        var events = await _github.GetUpdates(owner, repo, link.LastNotifiedAt);

        if (events.Count == 0)
        {
            return null;
        }

        _logger.LogInformation("Найдены обновления для GitHub репозитория {Owner}/{Repo}: {Count} событий.", owner, repo, events.Count);
        return new LinkUpdateDto
        {
            Id = link.Id,
            Url = link.Url,
            NewUpdatedAt = events.Max(e => e.CreatedAt),
            Description = "Обновление GitHub репозитория",
            ChatId = link.ChatId,
            Events = events
        };
    }

    private async Task<LinkUpdateDto?> CheckStackOverflow(LinkDto link)
    {
        var id = ParseQuestionId(link.Url);
        if (id is null)
        {
            return null;
        }

        var events = await _stack.GetUpdates(id.Value, link.LastNotifiedAt);

        if (events.Count == 0)
        {
            return null;
        }

        return new LinkUpdateDto
        {
            Id = link.Id,
            Url = link.Url,
            Description = "Новая активность StackOverflow",
            ChatId = link.ChatId,
            NewUpdatedAt = events.Max(e => e.CreatedAt),
            Events = events
        };
    }

    private static long? ParseQuestionId(string url)
    {
        var uri = new Uri(url);
        var parts = uri.AbsolutePath.Split('/');

        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i] == "questions" && i + 1 < parts.Length &&
                long.TryParse(parts[i + 1], out var id))
            {
                return id;
            }
        }

        return null;
    }

    private static (string? owner, string? repo) ParseGitHub(string url)
    {
        var uri = new Uri(url);
        var parts = uri.AbsolutePath.Trim('/').Split('/');

        if (parts.Length < 2)
        {
            return (null, null);
        }

        return (parts[0], parts[1]);
    }
}
