using System.Text;
using System.Text.Json;
using System.Web;
using QQLike.Entity.Common;
using QQLike.Entity.Configuration; 
using QQLike.Entity.Result;
using QQLike.Entity.VO;
using QQLike.Functional.Instructure;

namespace QQLike.Functional;

/// <summary>
/// 一般JSON内容API请求,登录之后增加授权头
/// </summary>
/// <param name="httpService"></param>
public class ApiService(
    IHttpService httpService,
    ISessionStorage sessionStorage,
    SysSetting setting) : IApiService
{
    private async Task<ResponseResult<TR>> Request<TR>(string apiUrl, object? model, HttpMethod method,
        Dictionary<string, string>? headers = null)
    {
        var json = JsonSerializer.Serialize(model);

        var tokenInfo = sessionStorage.Get<UserLoginVO>(CachingKeys.User);
        if (headers == null)
            headers = new Dictionary<string, string> { { "Authorization", $"Bearer {tokenInfo.Token}" } };
        else
        {
            headers["Authorization"] = $"Bearer {tokenInfo.Token}";
        }
        using var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
        var requestUrl = $"{setting.ApiUrl}/{apiUrl}";
        if (method == HttpMethod.Get || method == HttpMethod.Delete)
        {
            var uriBuilder = new UriBuilder(requestUrl);
            if (model != null)
            {
                var query = HttpUtility.ParseQueryString(uriBuilder.Query);

                var properties = model.GetType().GetProperties();
                foreach (var property in properties)
                {
                    var value = property.GetValue(model);
                    query[property.Name] = value?.ToString();
                }
                uriBuilder.Query = query.ToString();
            }

            var url = uriBuilder.ToString();
            var resultStr = await httpService.Request(url, method, httpContent, headers);
            var res = JsonSerializer.Deserialize<ResponseResult<TR>>(resultStr,Constants.DesSerializerOptions);
            return res;
        }
        else
        {
            
            var resultStr = await httpService.Request(requestUrl, method, httpContent, headers);
            var res = JsonSerializer.Deserialize<ResponseResult<TR>>(resultStr, Constants.DesSerializerOptions);
            return res;
        }
    }

    public Task<ResponseResult<TR>> PostAsync<TR>(string apiUrl, object? model,
        Dictionary<string, string>? headers = null)
    {
        return Request<TR>(apiUrl, model, HttpMethod.Post, headers);
    }

    public Task<ResponseResult<TR>> GetAsync<TR>(string apiUrl, object? model,
        Dictionary<string, string>? headers = null)
    {
        return Request<TR>(apiUrl, model, HttpMethod.Get, headers);
    }

    public Task<ResponseResult<TR>> PutAsync<TR>(string apiUrl, object? model,
        Dictionary<string, string>? headers = null)
    {
        return Request<TR>(apiUrl, model, HttpMethod.Put, headers);     
    }

    public Task<ResponseResult<TR>> PatchAsync<TR>(string apiUrl, object? model,
        Dictionary<string, string>? headers = null)
    {
        return Request<TR>(apiUrl, model, HttpMethod.Patch, headers);
    }

    public Task<ResponseResult<TR>> DeleteAsync<TR>(string apiUrl, object? model,   
        Dictionary<string, string>? headers = null)
    {
        return Request<TR>(apiUrl, model, HttpMethod.Delete, headers);
    }
}