using System.Text.Json;
using QQLike.Services.Interfaces;
using StackExchange.Redis;

namespace QQLike.Services;

public class RedisCache(IConnectionMultiplexer redisConnection) : IRedisCache
{
    public T? Get<T>(string key)
    {
       var value = redisConnection.GetDatabase().StringGet(key);
       if (value.HasValue)
           return JsonSerializer.Deserialize<T>(value.ToString());
       return default;
    }

    public void Set<T>(string key, T value, TimeSpan? expire = null,When when = When.Always)
    {
        var valueToStore = JsonSerializer.Serialize(value);
        redisConnection.GetDatabase().StringSet(key, valueToStore, expire, when: when);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
       var value = await redisConnection.GetDatabase().StringGetAsync(key);
       if (value.HasValue)
           return JsonSerializer.Deserialize<T>(value.ToString());
       return default;
    }

    public async Task SetAsync<T>(string key, T value,TimeSpan? expire = null, When when = When.Always)
    {
        var valueToStore = JsonSerializer.Serialize(value);
        await redisConnection.GetDatabase().StringSetAsync(key, valueToStore, expire, when: when);
    }

    public bool Exists(string key)
    {
        return redisConnection.GetDatabase().KeyExists(key);
    }

    public async Task<bool> ExistsAsync(string key)
    {
        return await redisConnection.GetDatabase().KeyExistsAsync(key);
    }

    public bool Remove(string key)
    {
       return redisConnection.GetDatabase().KeyDelete(key);
    }

    public async Task<bool> RemoveAsync(string key)
    {
        return await redisConnection.GetDatabase().KeyDeleteAsync(key);
    }

    public bool RemoveByPattern(string pattern)
    {
        var server = redisConnection.GetServer(redisConnection.GetEndPoints().First());
        var keys = server.Keys(pattern: pattern);
        var res = true;
        foreach (var key in keys)
            res = res && Remove(key);
        
        return res;
    }

    public async Task<bool> RemoveByPatternAsync(string pattern)
    {
        var server = redisConnection.GetServer(redisConnection.GetEndPoints().First());
        var keys = server.Keys(pattern: pattern);
        var res = true;
        foreach (var key in keys)
           res = res && await RemoveAsync(key);
        
        return res;
    }

    public void SetIf<T>(string key, T value)
    {
        redisConnection.GetDatabase().StringSet(key, JsonSerializer.Serialize(value), when: When.NotExists);
    }

    public async Task SetIfAsync<T>(string key, T value)
    {
        await redisConnection.GetDatabase().StringSetAsync(key, JsonSerializer.Serialize(value), when: When.NotExists);
    }
}