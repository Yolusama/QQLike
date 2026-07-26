namespace  QQLike.Functional.Instructure;

public interface IProjectLogger
{
    public string BasePath { get; }
    public void Log(string content, string to);
    public Task LogAsync(string content, string to);
}