using System.Collections.Frozen;
using QQLike.Functional.Instructure;

namespace QQLike.Functional;

public class SessionStorage : ISessionStorage
{
    private readonly Dictionary<string,object> _storage = new ();
    private FrozenDictionary<string, object> FrozenStorage => _storage.ToFrozenDictionary();
    public T Get<T>(string key)
    {
       if(FrozenStorage.TryGetValue(key, out var value))
           return (T)value;
       return default!;
    }

    public void Set<T>(string key, T value)
    {
        if(FrozenStorage.ContainsKey(key))
            _storage[key] = value;
        else
            _storage.Add(key, value);
    }

    public bool KeyExists(string key)
    {
        return FrozenStorage.ContainsKey(key);
    }

    public bool Remove(string key)
    {
        return _storage.Remove(key);
    }
}