namespace LinkTracker.Scrapper.Domain.DTOs.GitHub;

using System.Text.Json.Serialization;

public class GitHubPrResponse
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("body")]
    public string Body { get; set; } = "";

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("user")]
    public GitHubUser User { get; set; } = new();
}
