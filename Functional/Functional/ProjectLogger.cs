using System.Text;
using QQLike.Functional.Instructure;

namespace QQLike.Functional;

public class ProjectLogger : IProjectLogger
{
    public string BasePath { get; }
    public ProjectLogger(string basePath)
    {
        BasePath = basePath;
        var directoryInfo = new DirectoryInfo(BasePath);
        if (!directoryInfo.Exists)
            directoryInfo.Create();
    }

    public void Log(string content, string to)
    {
        var actualPath = Path.Combine(BasePath, to);
        var directoryInfo = new DirectoryInfo(actualPath);
        if (!directoryInfo.Exists)
            directoryInfo.Create();
        var toWrite = $"----{DateTime.Now:yyyy-MM-dd HH:mm:ss}----\r\n{content}\r\n";
        var fileStream = new FileStream(Path.Combine(actualPath,$"{DateTime.Today:yyyy-MM-dd}.log"), 
            FileMode.Append, FileAccess.Write,FileShare.ReadWrite);
        fileStream.Write(Encoding.UTF8.GetBytes(toWrite));
        fileStream.Flush();
    }

    public async Task LogAsync(string content, string to)
    {
        var actualPath = Path.Combine(BasePath, to);
        var directoryInfo = new DirectoryInfo(actualPath);
        if (!directoryInfo.Exists)
            directoryInfo.Create();
        var toWrite = $"----{DateTime.Now:yyyy-MM-dd HH:mm:ss}----\r\n{content}\r\n";
        var fileStream = new FileStream(Path.Combine(actualPath,$"{DateTime.Today:yyyy-MM-dd}.log"), 
            FileMode.Append, FileAccess.Write,FileShare.ReadWrite);
       await fileStream.WriteAsync(Encoding.UTF8.GetBytes(toWrite));
       await fileStream.FlushAsync();
    }
}