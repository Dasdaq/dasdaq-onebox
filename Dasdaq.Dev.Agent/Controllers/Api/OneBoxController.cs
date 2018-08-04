using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Dasdaq.Dev.Agent.Models;
using Newtonsoft.Json;

namespace Dasdaq.Dev.Agent.Controllers.Api
{
    [Route("api/[controller]")]
    public class OneBoxController : BaseController
    {
        [HttpPost("stop")]
        public ApiResult Stop()
        {
            Task.Run(async () => {
                await Task.Delay(1000);
                Environment.Exit(0);
            });
            return ApiResult(202, "Accepted");
        }

        [HttpGet("config")]
        public ApiResult<Config> Config()
        {
            var config = System.IO.File.ReadAllText("config.json");
            return ApiResult(JsonConvert.DeserializeObject<Config>(config));
        }

        [HttpPut("config")]
        [HttpPost("config")]
        [HttpPatch("config")]
        public ApiResult Config([FromBody] Config config)
        {
            System.IO.File.WriteAllText("config.json", JsonConvert.SerializeObject(config));
            return ApiResult(200, "Succeeded");
        }
    }
}
