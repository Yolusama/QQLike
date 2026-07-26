namespace QQLike.Functional.Instructure;

public interface ILocalStorage
{
    public T Get<T>(string key);
    public void Set<T>(string key, T value);
    public bool KeyExists(string key);
    public bool Remove(string key);
}