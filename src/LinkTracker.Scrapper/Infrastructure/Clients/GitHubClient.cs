using LinkTracker.Contracts;
using LinkTracker.Contracts.Enums;
using LinkTracker.Scrapper.Application.Interfaces;
using LinkTracker.Scrapper.Application.Services;
using LinkTracker.Scrapper.Domain.DTOs.GitHub;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace LinkTracker.Scrapper.Infrastructure.Clients;

public class GitHubClient(HttpClient httpClient, ILogger<GitHubClient> logger) : IGitHubClient
{
    private const string BaseUrl = "repos";
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<List<UpdateEvent>> GetUpdates(string owner, string repo, DateTime? since)
    {
        var updates = new List<UpdateEvent>();

        var prs = await GetPullRequests(owner, repo, since);
        var issues = await GetIssues(owner, repo, since);
        var comments = await GetComments(owner, repo, since);

        updates.AddRange(prs);
        updates.AddRange(issues);
        updates.AddRange(comments);

        logger.LogInformation("Всего получено {Count} обновлений для репозитория {Owner}/{Repo}", updates.Count, owner, repo);
        return updates;
    }

    private async Task<List<UpdateEvent>> GetPullRequests(string owner, string repo, DateTime? since)
    {
        var url = $"{BaseUrl}/{owner}/{repo}/pulls?state=all&per_page=100&sort=updated&direction=desc";

        var prs = await SafeGet<List<GitHubPrResponse>>(url) ?? [];

        return [.. prs
            .Where(p => !since.HasValue || p.CreatedAt > since)
            .Select(p => new UpdateEvent
            {
                Title = p.Title,
                Author = p.User?.Login ?? "Unknown",
                CreatedAt = p.CreatedAt,
                Text = Trim(p.Body),
                EventType = EventType.PR
            })];
    }

    private async Task<List<UpdateEvent>> GetIssues(string owner, string repo, DateTime? since)
    {
        var url = $"{BaseUrl}/{owner}/{repo}/issues?state=all&per_page=100&sort=updated&direction=desc";

        var issues = await SafeGet<List<GitHubIssueResponse>>(url) ?? [];

        return [.. issues
            .Where(i => i.PullRequest == null)
            .Where(i => !since.HasValue || i.CreatedAt > since)
            .Select(i => new UpdateEvent
            {
                Title = i.Title,
                Author = i.User?.Login ?? "Unknown",
                CreatedAt = i.CreatedAt,
                Text = Trim(i.Body),
                EventType = EventType.Issue
            })];
    }

    private async Task<List<UpdateEvent>> GetComments(string owner, string repo, DateTime? since)
    {
        var url = $"{BaseUrl}/{owner}/{repo}/issues/comments?per_page=100&sort=updated&direction=desc";

        var comments = await SafeGet<List<GitHubCommentResponse>>(url) ?? [];

        return [.. comments
            .Where(c => !since.HasValue || c.CreatedAt > since)
            .Select(c => new UpdateEvent
            {
                Title = "Новый комментарий",
                Author = c.User?.Login ?? "Unknown",
                CreatedAt = c.CreatedAt,
                Text = Trim(c.Body),
                EventType = EventType.Comment
            })];
    }

    private static string Trim(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        var plainText = System.Text.RegularExpressions.Regex.Replace(text, "<.*?>", string.Empty);
        return plainText.Trim();
    }

    private async Task<T?> SafeGet<T>(string url)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await httpClient.GetAsync(url);
            stopwatch.Stop();

            MetricsService.RequestDurationHistogram.WithLabels("external_source", "github.com", "scrapper").Observe(stopwatch.ElapsedMilliseconds / 1000.0);
            MetricsService.ApiRequestsTotal.WithLabels("github.com", "scrapper").Inc();

            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                logger.LogWarning("GitHub rate limit exceeded");
                return default;
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return default;
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("GitHub request failed: {Status}", response.StatusCode);
                return default;
            }

            return await response.Content.ReadFromJsonAsync<T>(_jsonSerializerOptions);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            MetricsService.RequestDurationHistogram.WithLabels("external_source", "github.com", "scrapper").Observe(stopwatch.ElapsedMilliseconds / 1000.0);
            logger.LogError(ex, "GitHub request error");
            throw;
        }
    }
}