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
        private static LaunchStatus _status = LaunchStatus.未启动;
        
        [HttpPut("init")]
        [HttpPost("init")]
        [HttpPatch("init")]
        public async Task<ApiResult> Init(bool? safeMode)
        {
            if (_status != LaunchStatus.未启动 && _status != LaunchStatus.启动失败)
            {
                return ApiResult(409, $"The EOS is under {_status} status.");
            }

            Task.Factory.StartNew(() => {
                using (var serviceScope = HttpContext.RequestServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
                using (var eos = serviceScope.ServiceProvider.GetService<EosService>())
                {
                    _status = LaunchStatus.正在启动;
                    if (eos.Launch(safeMode.HasValue ? safeMode.Value : false))
                    {
                        _status = LaunchStatus.正在运行;
                    }
                    else
                    {
                        _status = LaunchStatus.启动失败;
                    }
                }
            });


            var instances = JsonConvert.DeserializeObject<Config>(System.IO.File.ReadAllText("config.json")).dapp;
            await Task.Factory.StartNew(() => {
                using (var serviceScope = HttpContext.RequestServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
                using (var ins = serviceScope.ServiceProvider.GetService<DappService>())
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

        [HttpPut("stop")]
        [HttpPost("stop")]
        [HttpPatch("stop")]
        public ApiResult Stop(bool? safeMode, [FromServices] EosService eos)
        {
            if (_status != LaunchStatus.正在运行 && _status != LaunchStatus.正在启动)
            {
                return ApiResult(409, $"The EOS is under {_status} status.");
            }

            if (safeMode.HasValue && safeMode.Value)
            {
                eos.GracefulShutdown();
            }
            else
            {
                eos.ForceShutdown();
            }
            _status = LaunchStatus.未启动;
            return ApiResult(200, "Succeeded");
        }

        [HttpGet("status")]
        public async Task<ApiResult<GetEosStatusResponse>> Status([FromServices] EosService eos)
        {
            string chainId = null;
            if (_status == LaunchStatus.正在运行)
            {
                chainId = await eos.RetriveChainIdAsync();
            }
            return ApiResult(new GetEosStatusResponse
            {
                Status = _status.ToString(),
                ChainId = chainId,
                LogStreamId = eos.GetOneBoxProcId()
            });
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
            var contract = ef.Contracts.SingleOrDefault(x => x.Name == id);
            if (contract != null && contract.Cpp.TrimEnd() == model.Cpp.TrimEnd()
                && contract.Abi == model.Abi)
            {
                return ApiResult(400, "合约内容没有变化，不能进行Patch");
            }

            Task.Factory.StartNew(() => {
                using (var serviceScope = services.GetRequiredService<IServiceScopeFactory>().CreateScope())
                using (var _eos = serviceScope.ServiceProvider.GetService<EosService>())
                {
                    _eos.SaveContract(id, model.Cpp, model.Abi, model.Hpp);
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

        [HttpGet("account")]
        public ApiResult<IEnumerable<string>> Account([FromServices] AgentContext ef)
        {
            return ApiResult<IEnumerable<string>>(ef.Accounts.Select(x => x.Name).ToList());
        }

        [HttpGet("currency")]
        public ApiResult<IEnumerable<string>> Currency([FromServices] AgentContext ef)
        {
            return ApiResult<IEnumerable<string>>(ef.Currencies.Select(x => x.Name).ToList());
        }

        [HttpPut("currency/{currency}")]
        public ApiResult Currency(
            string currency, [FromServices] EosService eos,
            [FromBody] PutCurrencyRequest request)
        {
            eos.CreateCurrency(currency, request.account, request.amount);
            return ApiResult(200, "Succeeded");
        }

        [HttpPost("currency/{currency}/account/{account}")]
        public ApiResult Currency(string currency, string account,
            [FromBody] PostCurrencyRequest request, [FromServices] EosService eos)
        {
            eos.IssueCurrency(currency, account, request.amount);
            return ApiResult(200, "Succeeded");
        }
    }
}
