using System.Text.Json.Serialization;

namespace LinkTracker.Scrapper.Domain.DTOs.StackOverflow;

public class AnswerResponse
{
    [JsonPropertyName("answer_id")]
    public long AnswerId { get; set; }

    [JsonPropertyName("creation_date")]
    public long CreationDateUnix { get; set; }

    [JsonIgnore]
    public DateTime CreationDate =>
        DateTimeOffset.FromUnixTimeSeconds(CreationDateUnix).UtcDateTime;

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("owner")]
    public OwnerResponse Owner { get; set; } = default!;
}
