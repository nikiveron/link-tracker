using System.Text.Json.Serialization;

namespace LinkTracker.Scrapper.Domain.DTOs.GitHub;

public class GitHubUser
{
    [JsonPropertyName("login")]
    public string Login { get; set; } = "";
}
