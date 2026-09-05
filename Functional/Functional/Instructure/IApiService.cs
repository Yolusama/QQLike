using QQLike.Entity.Result;

namespace QQLike.Functional.Instructure;

public interface IApiService
{
    public IHttpService HttpService { get; }
    public Task<ResponseResult<TR>> PostAsync<TR>(string apiUrl, object? model,Dictionary<string, string>? headers = null);
    public Task<ResponseResult<TR>> GetAsync<TR>(string apiUrl, object? model,Dictionary<string, string>? headers = null);
    public Task<ResponseResult<TR>> PutAsync<TR>(string apiUrl, object? model,Dictionary<string, string>? headers = null);
    public Task<ResponseResult<TR>> PatchAsync<TR>(string apiUrl, object? model,Dictionary<string, string>? headers = null);
    public Task<ResponseResult<TR>> DeleteAsync<TR>(string apiUrl, object? model,Dictionary<string, string>? headers = null);
}