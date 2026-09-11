using System.Text.Json.Serialization;

namespace LinkTracker.Scrapper.Domain.DTOs.StackOverflow;

public class OwnerResponse
{
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;
}
