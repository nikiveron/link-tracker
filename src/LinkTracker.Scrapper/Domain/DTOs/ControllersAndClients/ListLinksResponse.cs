using System.Text.Json.Serialization;

namespace LinkTracker.Scrapper.Domain.DTOs.ControllersAndClients;

public class ListLinksResponse
{
    [JsonPropertyName("links")]
    public List<LinkResponse> Links { get; set; } = [];

    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }
}
