using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LinkTracker.Scrapper.Domain.DTOs.ControllersAndClients;

public class RemoveLinkRequest
{
    [Url]
    [JsonPropertyName("link")]
    public string Link { get; set; } = string.Empty;
}
