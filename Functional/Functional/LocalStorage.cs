using QQLike.Functional.Instructure;
using System.Text.Json;

namespace QQLike.Functional;

public class LocalStorage : ILocalStorage
{
    private readonly object _syncRoot = new();
    private readonly string _cacheDirectoryPath;
    private readonly string _cacheFilePath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private Dictionary<string, JsonElement> _storage = new();

    public LocalStorage()
    {
        var rootPath = Directory.GetCurrentDirectory();
        _cacheDirectoryPath = Path.Combine(rootPath, "Cache");
        _cacheFilePath = Path.Combine(_cacheDirectoryPath, "data.json");

        EnsureCacheFileExists();
        LoadFromFile();
    }

    public T Get<T>(string key)
    {
        lock (_syncRoot)
        {
            if (!_storage.TryGetValue(key, out var value))
            {
                return default!;
            }

            try
            {
                return value.Deserialize<T>(_jsonOptions)!;
            }
            catch
            {
                return default!;
            }
        }
    }

    public void Set<T>(string key, T value)
    {
        lock (_syncRoot)
        {
            _storage[key] = SerializeToJsonElement(value);
            SaveToFile();
        }
    }

    public bool KeyExists(string key)
    {
        lock (_syncRoot)
        {
            return _storage.ContainsKey(key);
        }
    }

    public bool Remove(string key)
    {
        lock (_syncRoot)
        {
            var removed = _storage.Remove(key);
            if (removed)
                SaveToFile();

            return removed;
        }
    }

    private void EnsureCacheFileExists()
    {
        Directory.CreateDirectory(_cacheDirectoryPath);
        if (!File.Exists(_cacheFilePath))
            File.WriteAllText(_cacheFilePath, "{}");
    }

    private void LoadFromFile()
    {
        lock (_syncRoot)
        {
            try
            {
                var json = File.ReadAllText(_cacheFilePath);
                _storage = string.IsNullOrWhiteSpace(json)
                    ? new Dictionary<string, JsonElement>()
                    : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, _jsonOptions) ?? new Dictionary<string, JsonElement>();
            }
            catch
            {
                _storage = new Dictionary<string, JsonElement>();
                SaveToFile();
            }
        }
    }

    private void SaveToFile()
    {
        lock (_syncRoot)
        {
            var json = JsonSerializer.Serialize(_storage, _jsonOptions);
            File.WriteAllText(_cacheFilePath, json);
        }
    }

    private static JsonElement SerializeToJsonElement<T>(T value)
    {
        var json = JsonSerializer.Serialize(value);
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
    
}