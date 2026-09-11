using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace LinkTracker.Scrapper.Domain.DTOs.ControllersAndClients;

public class LinkResponse
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [Url]
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    public override string ToString()
    {
        var tagsText = Tags.Count > 0
            ? $"🏷️ Теги: {string.Join(", ", Tags)}"
            : "🏷️ Теги: не указаны";

        return $"🔗 Ссылка: {Url}\n{tagsText}";
    }
}