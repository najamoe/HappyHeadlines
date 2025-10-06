using StackExchange.Redis;
using System.Text.Json;

public class RedisCacheService
{
    private readonly IDatabase _db;


    public RedisCacheService(string connectionString)
    {
        var connection = ConnectionMultiplexer.Connect(connectionString);
        _db = connection.GetDatabase();
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var json = JsonSerializer.Serialize(value);
        await _db.StringSetAsync(key, json, expiry);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var json = await _db.StringGetAsync(key);
        return json.HasValue ? JsonSerializer.Deserialize<T>(json!) : default;
    }

    public async Task RemoveAsync(string key)
    {
        await _db.KeyDeleteAsync(key);
    }
}
