using LinkTracker.Contracts;
using LinkTracker.Contracts.Enums;
using LinkTracker.Scrapper.Application.Interfaces;
using LinkTracker.Scrapper.Application.Services;
using LinkTracker.Scrapper.Domain.DTOs.StackOverflow;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LinkTracker.Scrapper.Infrastructure.Clients;

public class StackOverflowClient : IStackOverflowClient
{
    private readonly string _stackOverflowApiUrl = "https://api.stackexchange.com/2.3/questions";
    private readonly HttpClient _httpClient;
    private readonly ILogger<StackOverflowClient> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public StackOverflowClient(HttpClient httpClient, ILogger<StackOverflowClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "LinkTracker-Bot/1.0");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<List<UpdateEvent>> GetUpdates(long questionId, DateTime? since)
    {
        var result = new List<UpdateEvent>();

        var question = await GetQuestionInfo(questionId);

        result.AddRange(await GetAnswers(questionId, since, question?.Title ?? ""));
        result.AddRange(await GetComments(questionId, since, question?.Title ?? ""));

        return [.. result.OrderBy(x => x.CreatedAt)];
    }

    private async Task<QuestionResponse?> GetQuestionInfo(long id)
    {
        var url = $"{_stackOverflowApiUrl}/{id}?site=stackoverflow&filter=withbody";
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var resp = await _httpClient.GetFromJsonAsync<StackOverflowResponse<QuestionResponse>>(url, _jsonSerializerOptions);
            stopwatch.Stop();
            MetricsService.RequestDurationHistogram.WithLabels("external_source", "stackoverflow.com", "scrapper").Observe(stopwatch.ElapsedMilliseconds / 1000.0);
            MetricsService.ApiRequestsTotal.WithLabels("stackoverflow.com", "scrapper").Inc();
            return resp?.Items?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            MetricsService.RequestDurationHistogram.WithLabels("external_source", "stackoverflow.com", "scrapper").Observe(stopwatch.ElapsedMilliseconds / 1000.0);
            _logger.LogError(ex, "Failed to get question info for {QuestionId}", id);
            return null;
        }
    }

    private async Task<List<UpdateEvent>> GetAnswers(long id, DateTime? since, string questionTitle)
    {
        var url = $"{_stackOverflowApiUrl}/{id}/answers?site=stackoverflow&filter=withbody&pagesize=100";
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var resp = await _httpClient.GetFromJsonAsync<StackOverflowResponse<AnswerResponse>>(url, _jsonSerializerOptions);
            stopwatch.Stop();
            MetricsService.RequestDurationHistogram.WithLabels("external_source", "stackoverflow.com", "scrapper").Observe(stopwatch.ElapsedMilliseconds / 1000.0);
            MetricsService.ApiRequestsTotal.WithLabels("stackoverflow.com", "scrapper").Inc();

            return resp?.Items?
                .Where(a => !since.HasValue || a.CreationDate > since)
                .Select(a => new UpdateEvent
                {
                    Title = questionTitle,
                    Author = a.Owner?.DisplayName ?? "Unknown",
                    CreatedAt = a.CreationDate,
                    Text = TrimAndCleanHtml(a.Body),
                    EventType = EventType.Answer
                }).ToList() ?? [];
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            MetricsService.RequestDurationHistogram.WithLabels("external_source", "stackoverflow.com", "scrapper").Observe(stopwatch.ElapsedMilliseconds / 1000.0);
            _logger.LogError(ex, "Failed to get answers for question {QuestionId}", id);
            return [];
        }
    }

    private async Task<List<UpdateEvent>> GetComments(long id, DateTime? since, string questionTitle)
    {
        var url = $"{_stackOverflowApiUrl}/{id}/comments?site=stackoverflow&filter=withbody&pagesize=100";
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var resp = await _httpClient.GetFromJsonAsync<StackOverflowResponse<CommentResponse>>(url, _jsonSerializerOptions);
            stopwatch.Stop();
            MetricsService.RequestDurationHistogram.WithLabels("external_source", "stackoverflow.com", "scrapper").Observe(stopwatch.ElapsedMilliseconds / 1000.0);
            MetricsService.ApiRequestsTotal.WithLabels("stackoverflow.com", "scrapper").Inc();

            return resp?.Items?
                .Where(c => !since.HasValue || c.CreationDate > since)
                .Select(c => new UpdateEvent
                {
                    Title = questionTitle,
                    Author = c.Owner?.DisplayName ?? "Unknown",
                    CreatedAt = c.CreationDate,
                    Text = TrimAndCleanHtml(c.Body),
                    EventType = EventType.Comment
                }).ToList() ?? [];
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            MetricsService.RequestDurationHistogram.WithLabels("external_source", "stackoverflow.com", "scrapper").Observe(stopwatch.ElapsedMilliseconds / 1000.0);
            _logger.LogError(ex, "Failed to get comments for question {QuestionId}", id);
            return [];
        }
    }

    private static string TrimAndCleanHtml(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        var plainText = Regex.Replace(text, "<.*?>", string.Empty);

        plainText = System.Net.WebUtility.HtmlDecode(plainText);

        plainText = Regex.Replace(plainText, @"\s+", " ");

        return plainText.Trim();
    }
}