namespace QQLike.Entity.Configuration.Server;

public class SysSetting
{
    public string AppName { get; set; }
    public string Version { get; set; }
    public string DbConnectionString { get; set; }
    public string LogPath { get; set; }
    public int ServerPort { get; set; }
    public string RedisConnectionString { get; set; }
}