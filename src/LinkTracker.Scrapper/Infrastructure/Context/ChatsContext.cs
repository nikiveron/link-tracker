using Microsoft.EntityFrameworkCore;
using LinkTracker.Scrapper.Domain.Entities;

namespace LinkTracker.Scrapper.Infrastructure.Context;

public class ChatsContext : DbContext
{
    public DbSet<Chat> Chats { get; set; }
    public DbSet<Link> Links { get; set; }
    public DbSet<Tag> Tags { get; set; }

    public ChatsContext(DbContextOptions<ChatsContext> options) : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Link>()
            .HasMany(l => l.Tags)
            .WithMany(t => t.Links)
            .UsingEntity<Dictionary<string, object>>(
                "link_tag",
                j => j
                    .HasOne<Tag>()
                    .WithMany()
                    .HasForeignKey("tag_id"),
                j => j
                    .HasOne<Link>()
                    .WithMany()
                    .HasForeignKey("link_id"),
                j =>
                {
                    j.HasKey("link_id", "tag_id");
                });
    }
}
