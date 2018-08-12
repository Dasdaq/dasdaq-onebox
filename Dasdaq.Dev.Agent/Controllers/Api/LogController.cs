using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Dasdaq.Dev.Agent.Services;

namespace Dasdaq.Dev.Agent.Controllers.Api
{
    [Route("api/[controller]")]
    public class LogController : BaseController
    {
        [HttpGet("{id:Guid}")]
        public object Get(Guid id, [FromServices] ProcessService proc)
        {
            var process = proc.FindOneBoxProcessById(id);
            if (process == null)
            {
                return ApiResult(404, "Not Found");
            }
            return ApiResult(process.Logs.Where(x => x.Time >= DateTime.Now.AddMinutes(-5)));
        }
    }
}
