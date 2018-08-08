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
        public ApiResult Init()
        {
            if (_status != LaunchStatus.PendingLaunch)
            {
                return ApiResult(409, $"The EOS is under {_status} status.");
            }

            Task.Factory.StartNew(() => {
                using (var serviceScope = HttpContext.RequestServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
                using (var eos = serviceScope.ServiceProvider.GetService<EosService>())
                {
                    _status = LaunchStatus.Launching;
                    eos.Launch();
                    _status = LaunchStatus.Launched;
                }
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
            Task.Factory.StartNew(() => {
                using (var serviceScope = services.GetRequiredService<IServiceScopeFactory>().CreateScope())
                using (var _eos = serviceScope.ServiceProvider.GetService<EosService>())
                {
                    _eos.SaveContract(id, model.Cpp, model.Hpp);
                    _eos.CompileAndPublishContract(id);
                }
            });

            return ApiResult(200, "Succeeded");
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

        [HttpPut("wallet")]
        [HttpPost("wallet")]
        [HttpPatch("wallet")]
        public ApiResult Wallet([FromServices] EosService eos, [FromBody] PatchWalletRequest request)
        {
            if (request.Status != "Unlocked")
            {
                return ApiResult(400, "Invalid argument: " + request.Status);
            }

            eos.UnlockWallet();
            return ApiResult(200, "Succeeded");
        }


        private enum LaunchStatus
        {
            PendingLaunch,
            Launching,
            Launched
        }
    }
}
