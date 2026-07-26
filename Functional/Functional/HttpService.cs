using QQLike.Functional.Instructure;

namespace QQLike.Functional;

public class HttpService : IHttpService
{
    public async Task<string> Request(string url,HttpMethod method,HttpContent? content=null,Dictionary<string, string>? headers = null)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromMinutes(3);
        using var request = new HttpRequestMessage(method, url);
        if (headers != null)
        {
            foreach (var header in headers)
                request.Headers.Add(header.Key, header.Value);
        }
        if (content != null)
            request.Content = content;
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}