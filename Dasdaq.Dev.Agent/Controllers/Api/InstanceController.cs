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
        private static Dictionary<string, Process> _dic = new Dictionary<string, Process>();

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
            [FromServices] AgentContext ef, [FromServices] IServiceProvider services)
        {
            if (_dic.ContainsKey(id))
            {
                return ApiResult(409, "The instance is already existed.");
            }

            ef.Instances.Add(new Instance
            {
                UploadMethod = request.Method,
                Data = request.Data,
                Name = id,
                Status = InstanceStatus.Running
            });
            ef.SaveChanges();

            Task.Run(() => {
                using (var serviceScope = services.GetRequiredService<IServiceScopeFactory>().CreateScope())
                using (var _ef = serviceScope.ServiceProvider.GetService<AgentContext>())
                {
                    try
                    {
                        var _ins = serviceScope.ServiceProvider.GetRequiredService<InstanceService>();
                        switch (request.Method)
                        {
                            case InstanceUploadMethod.Git:
                                _ins.CloneGitRepo(id, request.Data);
                                break;
                            case InstanceUploadMethod.Zip:
                                _ins.ExtractZip(id, request.Data);
                                break;
                            default:
                                throw new NotSupportedException();
                        }
                        var process = _ins.StartInstance(id);
                        _dic.Add(id, process);
                        process.WaitForExit();
                        var _instance = _ef.Instances.Single(x => x.Name == id);
                        _instance.ExitCode = process.ExitCode;
                        _instance.ExitTime = DateTime.Now;
                        _instance.Status = _instance.ExitCode == 0 ? InstanceStatus.Succeeded : InstanceStatus.Failed;
                        _ef.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[Dasdaq Dev Agent] An error occurred: \r\n" + ex.ToString());
                    }
                }
            });

            return ApiResult(201, "Created");
        }

        [HttpDelete("{id}")]
        public ApiResult Delete(string id, [FromServices] AgentContext ef)
        {
            var instance = ef.Instances.SingleOrDefault(x => x.Name == id);
            if (instance == null)
            {
                return ApiResult(404, "Not Found");
            }

            if (_dic.ContainsKey(id))
            {
                _dic[id].Kill();
                _dic.Remove(id);
            }

            ef.Remove(instance);
            ef.SaveChanges();

            return ApiResult(200, "Succeeded");
        }
    }
}
