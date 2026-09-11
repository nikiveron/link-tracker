using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinkTracker.Scrapper.Domain.Entities;

[Table("chats")]
public class Chat
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    public List<Link> Links { get; set; } = [];
}