using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinkTracker.Scrapper.Domain.Entities;

[Table("tags")]
public class Tag
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    public List<Link> Links { get; set; } = [];
}
