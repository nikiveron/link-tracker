using System.ComponentModel.DataAnnotations;

namespace LinkTracker.Contracts;

public class LinkUpdateDto
{
    [Required(ErrorMessage = "Id is required")]
    public long Id { get; set; }

    [Required(ErrorMessage = "Url is required")]
    [Url(ErrorMessage = "Invalid URL format")]
    public string Url { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "ChatId is required")]
    public long ChatId { get; set; }

    public DateTime NewUpdatedAt { get; set; }
    public List<UpdateEvent> Events { get; set; } = [];
}