namespace QQLike.Entity.Configuration;

public class SysSetting
{
    public string AppName { get; set; }
    public string Version { get; set; }
    public string DbConnectionString { get; set; }
    public string LogPath { get; set; }
    public string RedisConnectionString { get; set; }
    public string ApiUrl { get; set; }
    public string SocketUrl { get; set; }
    public int SocketServerPort  { get; set; }
    public string FileStorePath { get; set; }
}