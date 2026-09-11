using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinkTracker.Scrapper.Domain.Entities;

[Table("links")]
public class Link
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("url")]
    public string Url { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("last_updated_at")]
    public DateTime? LastUpdatedAt { get; set; }

    [Column("last_notified_at")]
    public DateTime? LastNotifiedAt { get; set; }

    [Column("chat_id")]
    public long ChatId { get; set; }
    [Required]
    public Chat? Chat { get; set; }

    public List<Tag> Tags { get; set; } = [];
}