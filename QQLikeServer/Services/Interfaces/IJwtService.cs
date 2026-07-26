namespace QQLike.Services.Interfaces;

public interface IJwtService
{
    public string Generate<T>(T payload, TimeSpan expire);
    public T Parse<T>(string token);
}