namespace QQLike.Entity.DTO;

public class ResponseResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public int Code { get; set; }
    
    public static ResponseResult Fail(string msg = "fail", int code = 500)
    {
        return new ResponseResult { Success = false, Message = msg, Code = code };
    }
    
    public static ResponseResult OK(string msg = "OK", int code = 200)
    {
        return new ResponseResult { Success = true, Message = msg, Code = code };
    }

    public ResponseResult<T> Generic<T>()
    {
        return new ResponseResult<T> 
        { 
            Success = Success, 
            Message = Message, 
            Code = Code,
        };
    }
}

public class ResponseResult<T>  : ResponseResult
{
    public T Data { get; set; }

    public static ResponseResult<T> OK(T data)
    {
        return new ResponseResult<T> { Success = true, Message = "OK", Code = 200,Data = data };
    }
    
    public static ResponseResult<T> OK(string msg, T data = default(T))
    {
        return new ResponseResult<T> { Success = true, Message = msg, Code = 200,Data = data };
    }
    
}