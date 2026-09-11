using LinkTracker.Scrapper.Application.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace LinkTracker.Scrapper.Infrastructure.Services;

public class ValkeyCacheService(IConnectionMultiplexer connection) : ICacheService
{
    private readonly IDatabase _db = connection.GetDatabase();
    private readonly IConnectionMultiplexer _connectionMultiplexer = connection;

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await _db.StringGetAsync(key);

        return !value.HasValue ? default : JsonSerializer.Deserialize<T>(value!);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl)
    {
        var json = JsonSerializer.Serialize(value);
        await _db.StringSetAsync(key, json, ttl);
    }

    public async Task DeleteAsync(string key)
    {
        await _db.KeyDeleteAsync(key);
    }

    public async Task DeleteByPrefixAsync(string prefix)
    {
        var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints().First());
        var keys = server.Keys(pattern: $"{prefix}*");
        foreach (var key in keys)
        {
            await _db.KeyDeleteAsync(key);
        }
    }
}