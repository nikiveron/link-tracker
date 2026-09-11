using System.Text.Json;
using LinkTracker.Scrapper.Infrastructure.Services;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace LinkTracker.Tests.Valkey;

public class ValkeyTests : IAsyncLifetime
{
    private readonly RedisContainer _redis = new RedisBuilder().WithImage("redis:7").Build();

    private IConnectionMultiplexer _connection = null!;
    private ValkeyCacheService _cache = null!;

    public async Task InitializeAsync()
    {
        await _redis.StartAsync();

        _connection = await ConnectionMultiplexer.ConnectAsync(
            _redis.GetConnectionString());

        _cache = new ValkeyCacheService(_connection);
    }

    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
        await _redis.DisposeAsync();
    }

    [Fact]
    public async Task Should_Cache_List_Response_In_Expected_Format()
    {
        // Arrange
        var key = "links:123:page:1:size:5:tags:none";

        var response = new TestListResponse
        {
            TotalCount = 2,
            Links =
            [
            new TestLink { Id = 1, Url = "https://test1.com" },
            new TestLink { Id = 2, Url = "https://test2.com" }
        ]
        };

        // Act
        await _cache.SetAsync(key, response, TimeSpan.FromSeconds(60));

        var rawValue = await _connection
            .GetDatabase()
            .StringGetAsync(key);

        var deserialized =
            JsonSerializer.Deserialize<TestListResponse>(rawValue!);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(2, deserialized!.TotalCount);
        Assert.Equal("https://test1.com", deserialized.Links[0].Url);
    }

    [Fact]
    public async Task Should_Remove_Cache_By_Ttl_And_By_Invalidation()
    {
        var key = "links:999:page:1:size:5:tags:none";

        var value = new TestListResponse
        {
            TotalCount = 1,
            Links =
            [
            new TestLink { Id = 10, Url = "https://ttl-test.com" }
        ]
        };

        await _cache.SetAsync(key, value, TimeSpan.FromSeconds(2));

        var existsBeforeTtl =
            await _connection.GetDatabase().KeyExistsAsync(key);

        Assert.True(existsBeforeTtl);

        await Task.Delay(3000);

        var existsAfterTtl =
            await _connection.GetDatabase().KeyExistsAsync(key);

        Assert.False(existsAfterTtl);

        await _cache.SetAsync(key, value, TimeSpan.FromSeconds(60));

        var existsBeforeDelete =
            await _connection.GetDatabase().KeyExistsAsync(key);

        Assert.True(existsBeforeDelete);

        await _cache.DeleteByPrefixAsync("links:999:");

        var existsAfterDelete =
            await _connection.GetDatabase().KeyExistsAsync(key);

        Assert.False(existsAfterDelete);
    }
}

public class TestListResponse
{
    public int TotalCount { get; set; }
    public TestLink[] Links { get; set; } = [];
}

public class TestLink
{
    public long Id { get; set; }
    public string Url { get; set; } = string.Empty;
}