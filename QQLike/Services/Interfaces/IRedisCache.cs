using StackExchange.Redis;

namespace QQLike.Services.Interfaces;

public interface IRedisCache
{
    public T? Get<T>(string key);
    public void Set<T>(string key, T value ,TimeSpan? expire = null, When when = When.Always);
    public Task<T?> GetAsync<T>(string key);
    public Task SetAsync<T>(string key, T value, TimeSpan? expire = null, When when = When.Always);
    public bool Exists(string key);
    public Task<bool> ExistsAsync(string key);
    public bool Remove(string key);
    public Task<bool> RemoveAsync(string key);
    public bool RemoveByPattern(string pattern);
    public Task<bool> RemoveByPatternAsync(string pattern);
}