using LinkTracker.Scrapper.Domain.Repository;

namespace LinkTracker.Tests.DbTests;

[Collection("db")]
public class SqlRepositoryTests : IntegrationTestBase
{
    private readonly PostgresFixture _fixture;

    public SqlRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        _fixture.ResetDatabase().GetAwaiter().GetResult();
    }

    private SqlLinkRepository CreateRepo() => new(_fixture.ConnectionString);

    [Fact]
    public void AddLink_ShouldSave()
    {
        // Arrange
        var repo = CreateRepo();
        long chatId = 1;

        repo.AddChat(chatId);

        var link = new LinkDto
        {
            ChatId = chatId,
            Url = "https://test.com",
            Tags = ["tag1"]
        };

        // Act
        var result = repo.AddLink(link);
        var saved = repo.GetLink(chatId, link.Url);

        // Assert
        Assert.True(result);
        Assert.NotNull(saved);
    }

    [Fact]
    public void RemoveLink_ShouldDelete()
    {
        // Arrange
        var repo = CreateRepo();
        long chatId = 2;

        repo.AddChat(chatId);

        var link = new LinkDto { ChatId = chatId, Url = "https://test.com", Tags = [] };

        repo.AddLink(link);

        // Act
        var removed = repo.RemoveLink(chatId, link.Url);
        var exists = repo.GetLink(chatId, link.Url);

        // Assert
        Assert.True(removed);
        Assert.Null(exists);
    }

    [Fact]
    public void AddDuplicateLink_ShouldFail()
    {
        // Arrange
        var repo = CreateRepo();
        long chatId = 3;

        repo.AddChat(chatId);

        var link = new LinkDto { ChatId = chatId, Url = "https://test.com", Tags = [] };

        repo.AddLink(link);

        // Act
        var second = repo.AddLink(link);

        // Assert
        Assert.False(second);
    }

    [Fact]
    public void UpdateLink_ShouldUpdateFields()
    {
        // Arrange
        var repo = CreateRepo();
        long chatId = 4;

        repo.AddChat(chatId);

        var link = new LinkDto { ChatId = chatId, Url = "https://test.com", Tags = [] };
        repo.AddLink(link);

        link.LastUpdatedAt = DateTime.UtcNow;

        // Act
        var updated = repo.UpdateLink(link);
        var dbLink = repo.GetLink(chatId, link.Url);

        // Assert
        Assert.True(updated);
        Assert.NotNull(dbLink!.LastUpdatedAt);
    }
}