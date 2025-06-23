using System.Text.Json;
using Microsoft.Extensions.Options;
using Movies.Application.Abstractions.Caching;
using Movies.Infrastructure.Configuration;
using StackExchange.Redis;

namespace Movies.Infrastructure.Caching;

public class RedisCacheService(IConnectionMultiplexer redis, IOptions<RedisOptions> options)
    : ICacheService
{
    private readonly IDatabase _database = redis.GetDatabase();
    private readonly RedisOptions _options = options.Value;

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var fullKey = GetFullKey(key);
        var value = await _database.StringGetAsync(fullKey);

        return value.HasValue ? JsonSerializer.Deserialize<T>(value!) : null;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default) where T : class
    {
        var fullKey = GetFullKey(key);
        var serializedValue = JsonSerializer.Serialize(value);
        var expiryTime = expiry ?? TimeSpan.FromMinutes(_options.DefaultExpireTimeMinutes);

        await _database.StringSetAsync(fullKey, serializedValue, expiryTime);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var fullKey = GetFullKey(key);
        await _database.KeyDeleteAsync(fullKey);
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        var fullPattern = GetFullKey(pattern);
        var server = _database.Multiplexer.GetServer(_database.Multiplexer.GetEndPoints().First());
        var keys = server.Keys(pattern: fullPattern);

        var redisKeys = keys as RedisKey[] ?? keys.ToArray();
        if (redisKeys.Any())
        {
            await _database.KeyDeleteAsync(redisKeys.ToArray());
        }
    }

    private string GetFullKey(string key) => $"{_options.KeyPrefix}:{key}";
}