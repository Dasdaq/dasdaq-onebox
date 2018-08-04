using Microsoft.AspNetCore.Mvc;
using Dasdaq.Dev.Agent.Models;

namespace Dasdaq.Dev.Agent.Controllers
{
    public class BaseController : Controller
    {
        public ApiResult ApiResult(int code, string msg = null)
        {
            Response.StatusCode = code;
            return new ApiResult
            {
                code = code,
                data = null,
                msg = msg
            };
        }

        public ApiResult<T> ApiResult<T>(T data)
        {
            return new ApiResult<T>
            {
                code = 200,
                data = data,
                msg = null
            };
        }
    }
}
