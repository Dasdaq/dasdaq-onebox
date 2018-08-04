using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Dasdaq.Dev.Agent.Models;
using Dasdaq.Dev.Agent.Services;

namespace Dasdaq.Dev.Agent.Controllers.Api
{
    [Route("api/[controller]")]
    public class InstanceController : BaseController
    {
        [HttpGet]
        public ApiResult<IEnumerable<Instance>> Get([FromServices] AgentContext ef)
        {
            var instances = ef.Instances.ToList();
            return ApiResult<IEnumerable<Instance>>(instances);
        }
        
        [HttpGet("{id}")]
        public object Get(string id, [FromServices] AgentContext ef)
        {
            var instance = ef.Instances.SingleOrDefault(x => x.Name == id);
            if (instance == null)
            {
                return ApiResult(404, "Not Found");
            }

            return ApiResult(instance);
        }

        [HttpPut("{id}")]
        [HttpPost("{id}")]
        [HttpPatch("{id}")]
        public ApiResult Put(string id, [FromBody] PutInstanceRequestBody request,
            [FromServices] AgentContext ef, [FromServices] IServiceProvider services,
            [FromServices] InstanceService ins)
        {
            if (ins.IsInstanceExisted(id))
            {
                return ApiResult(409, "The instance is already existed.");
            }

            Task.Factory.StartNew(async () => {
                using (var serviceScope = services.GetRequiredService<IServiceScopeFactory>().CreateScope())
                using (var _ins = serviceScope.ServiceProvider.GetService<InstanceService>())
                {
                    await _ins.DownloadAndStartInstanceAsync(id, request.Method, request.Data);
                }
            });

            return ApiResult(201, "Created");
        }

        [HttpDelete("{id}")]
        public ApiResult Delete(string id, [FromServices] AgentContext ef, [FromServices] InstanceService ins)
        {
            var instance = ef.Instances.SingleOrDefault(x => x.Name == id);
            if (instance == null)
            {
                return ApiResult(404, "Not Found");
            }

            ins.StopInstance(id);

            return ApiResult(200, "Succeeded");
        }
    }
}
