using System.Buffers;
using System.Text;
using Microsoft.AspNetCore.Mvc.Filters;
using QQLike.Entity.Result;
using QQLike.Functional.Instructure;
using QQLike.Functional.Utils;

namespace QQLike.Services;

public class GlobalExceptionHandler(IProjectLogger logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        logger.Log($"出现异常：{context.Exception}","全局异常过滤器");
        context.HttpContext.Response.ContentType = "application/json";
        context.HttpContext.Response.BodyWriter.Write(Encoding.UTF8.GetBytes(ResponseResult.Fail(context.Exception.Message).ToNormalJson()));
    }
}