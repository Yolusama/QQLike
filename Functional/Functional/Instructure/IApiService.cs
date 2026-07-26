using QQLike.Entity.DTO;

namespace QQLike.Functional.Instructure;

public interface IApiService
{
    public Task<ResponseResult<TR>> PostAsync<TR,TM>(string apiUrl, TM model,Dictionary<string, string>? headers = null);
    public Task<ResponseResult<TR>> GetAsync<TR,TM>(string apiUrl,TM model,Dictionary<string, string>? headers = null);
    public Task<ResponseResult<TR>> PutAsync<TR,TM>(string apiUrl, TM model,Dictionary<string, string>? headers = null);
    public Task<ResponseResult<TR>> PatchAsync<TR,TM>(string apiUrl, TM model,Dictionary<string, string>? headers = null);
    public Task<ResponseResult<TR>> DeleteAsync<TR,TM>(string apiUrl,TM model,Dictionary<string, string>? headers = null);
}