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
        if(expire == null)
            redisConnection.GetDatabase().StringSet(key, valueToStore, when: when);
        else
        {
            var expiration = new Expiration(expire.Value);
            redisConnection.GetDatabase().StringSet(key, valueToStore, expiration, when);
        }
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
        if(expire == null)
            await redisConnection.GetDatabase().StringSetAsync(key, valueToStore,when:when);
        else
        {
            var expiration = new Expiration(expire.Value);
            await redisConnection.GetDatabase().StringSetAsync(key, valueToStore, expiration, when);
        }
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
}