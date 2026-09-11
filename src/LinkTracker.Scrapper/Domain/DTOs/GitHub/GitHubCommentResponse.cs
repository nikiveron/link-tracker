namespace LinkTracker.Scrapper.Domain.DTOs.GitHub;

using System.Text.Json.Serialization;

public class GitHubCommentResponse
{
    [JsonPropertyName("body")]
    public string Body { get; set; } = "";

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("user")]
    public GitHubUser User { get; set; } = new();
}
