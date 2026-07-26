using System.Text.Json;

namespace QQLike.Entity.Common;

public static class Constants
{
    public const int RegisterCodeLength = 6;
    public static TimeSpan TokenExpire => TimeSpan.FromDays(7);
    public static JsonSerializerOptions DesSerializerOptions => new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };
}