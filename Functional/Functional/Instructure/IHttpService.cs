namespace QQLike.Functional.Instructure;

public interface IHttpService
{
    public Task<string> Request(string url,HttpMethod method,HttpContent? content=null,Dictionary<string,string>? headers=null);

    public Task<byte[]> GetFileResult(string url, HttpContent? content = null, Dictionary<string, string>? headers = null);
}