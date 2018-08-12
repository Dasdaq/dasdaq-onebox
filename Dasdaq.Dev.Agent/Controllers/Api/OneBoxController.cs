using System;
using System.Collections.Generic;
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
            Task.Factory.StartNew(async () => {
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

        [HttpPut("config/plugins")]
        [HttpPost("config/plugins")]
        [HttpPatch("config/plugins")]
        public ApiResult ConfigPlugins([FromBody] IEnumerable<string> request)
        {
            var config = JsonConvert.DeserializeObject<Config>(System.IO.File.ReadAllText("config.json"));
            config.eos.plugins = request;
            System.IO.File.WriteAllText("config.json", JsonConvert.SerializeObject(config));
            return ApiResult(200, "Succeeded");
        }


        [HttpPut("config/keyPair")]
        [HttpPost("config/keyPair")]
        [HttpPatch("config/keyPair")]
        public ApiResult ConfigKeyPair([FromBody] ConfigEosKeyPair request)
        {
            var config = JsonConvert.DeserializeObject<Config>(System.IO.File.ReadAllText("config.json"));
            config.eos.keyPair = request;
            System.IO.File.WriteAllText("config.json", JsonConvert.SerializeObject(config));
            return ApiResult(200, "Succeeded");
        }
    }
}
