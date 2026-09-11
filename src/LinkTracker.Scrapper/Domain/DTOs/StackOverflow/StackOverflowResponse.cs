using System.Text.Json.Serialization;

namespace LinkTracker.Scrapper.Domain.DTOs.StackOverflow;

public class StackOverflowResponse<T>
{
    [JsonPropertyName("items")]
    public List<T> Items { get; set; } = [];

    [JsonPropertyName("has_more")]
    public bool HasMore { get; set; }

    [JsonPropertyName("quota_remaining")]
    public int QuotaRemaining { get; set; }
}
