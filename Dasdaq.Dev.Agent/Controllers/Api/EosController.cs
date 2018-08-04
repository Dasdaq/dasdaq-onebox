using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Dasdaq.Dev.Agent.Services;
using Dasdaq.Dev.Agent.Models;

namespace Dasdaq.Dev.Agent.Controllers.Api
{
    [Route("api/[controller]")]
    public class EosController : BaseController
    {
        private static LaunchStatus _status = LaunchStatus.PendingLaunch;
        
        [HttpPut("init")]
        [HttpPost("init")]
        [HttpPatch("init")]
        public ApiResult Init([FromServices] EosService eos)
        {
            if (_status != LaunchStatus.PendingLaunch)
            {
                return ApiResult(409, $"The EOS is under {_status} status.");
            }

            Task.Factory.StartNew(() => {
                _status = LaunchStatus.Launching;
                eos.Launch();
                _status = LaunchStatus.Launched;
            });


            var instances = JsonConvert.DeserializeObject<Config>(System.IO.File.ReadAllText("config.json")).Instances;
            Task.Factory.StartNew(() => {
                using (var serviceScope = HttpContext.RequestServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
                using (var ins = serviceScope.ServiceProvider.GetService<InstanceService>())
                {
                    if (instances == null)
                    {
                        return;
                    }
                    foreach (var x in instances)
                    {
                        ins.DownloadAndStartInstanceAsync(
                            System.IO.Path.GetFileNameWithoutExtension(x),
                            InstanceUploadMethod.Git,
                            x);
                    }
                }
            });

            return ApiResult(201, "Lauching...");
        }

        [HttpGet("status")]
        public ApiResult<string> Status()
        {
            return ApiResult(_status.ToString());
        }
        
        [HttpGet("contract")]
        public ApiResult<IEnumerable<Contract>> Contract([FromServices] AgentContext ef)
        {
            var contracts = ef.Contracts
                .OrderBy(x => x.DeployedTime)
                .ToList();

            return ApiResult<IEnumerable<Contract>>(contracts);
        }
        
        [HttpPut("contract/{id}")]
        [HttpPost("contract/{id}")]
        [HttpPatch("contract/{id}")]
        public ApiResult Contract(string id, [FromBody] Contract model, 
            [FromServices] IServiceProvider services, [FromServices] AgentContext ef)
        {
            Contract contract;
            var isCreate = false;
            if (ef.Contracts.Any(x => x.Name == id))
            {
                contract = ef.Contracts.Single(x => x.Name == id);
            }
            else
            {
                isCreate = true;
                contract = new Contract();
                contract.Name = id;
            }

            contract.Cpp = model.Cpp;
            contract.Hpp = model.Hpp;
            contract.DeployedTime = DateTime.Now;
            contract.Status = ContractStatus.Updating;
            ef.SaveChanges();

            Task.Factory.StartNew(() => {
                using (var serviceScope = services.GetRequiredService<IServiceScopeFactory>().CreateScope())
                using (var _ef = serviceScope.ServiceProvider.GetService<AgentContext>())
                {
                    var _eos = serviceScope.ServiceProvider.GetRequiredService<EosService>();
                    var _contract = _ef.Contracts.Single(x => x.Name == id);

                    try
                    {
                        _eos.SaveContract(id, contract.Cpp, contract.Hpp);
                        _eos.CompileAndPublishContract(id);
                    }
                    catch
                    {
                        _contract.Status = ContractStatus.Failed;
                        _ef.SaveChanges();
                        return;
                    }
                    
                    _contract.Status = ContractStatus.Available;
                    _ef.SaveChanges();
                }
            });
           
            return isCreate ? ApiResult(201, "Created") : ApiResult(202, "Accepted");
        }

        [HttpGet("contract/{id}")]
        public object Contract(string id, [FromServices] AgentContext ef)
        {
            var contract = ef.Contracts.SingleOrDefault(x => x.Name == id);
            if (contract == null)
            {
                return ApiResult(404, "Not Found");
            }

            return ApiResult(contract);
        }


        private enum LaunchStatus
        {
            PendingLaunch,
            Launching,
            Launched
        }
    }
}
