using System.Text.Json;

namespace QQLike.Entity.Common;

public static class Constants
{
    public const int RegisterCodeLength = 6;
    public const string TempFileSuffix = ".tmp";
    public static TimeSpan TokenExpire => TimeSpan.FromDays(7);
    public static JsonSerializerOptions DesSerializerOptions => new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };
    public const string MQExchange = "QQLike";
    public const string CreateChatGroupQueue =  "CreateChatGroup";
    public const string FileResponseHeader = "application/octet-stream";
    public const int KB = 1024;
    public const int MB = KB * KB;
    public const int GB = MB * KB;
}