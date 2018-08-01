using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Linq;
using System.IO;
using Newtonsoft.Json;

namespace Dasdaq.Dev.Agent.Services
{
    public class EosService
    {
        private HttpClient _client = new HttpClient() { BaseAddress = new Uri("http://localhost:8888") };

        public EosService()
        {
            if (!Directory.Exists("/home/dasdaq_eos"))
            {
                Directory.CreateDirectory("/home/dasdaq_eos");
            }

            if (!Directory.Exists("/home/dasdaq_eos/contracts"))
            {
                Directory.CreateDirectory("/home/dasdaq_eos/contracts");
            }

            if (!Directory.Exists("/home/dasdaq_eos/dev"))
            {
                Directory.CreateDirectory("/home/dasdaq_eos/dev");
            }
        }

        public Task StartEosNodeAsync()
        {
            return Task.Run(()=> {
                // Start bash to launch nodeos
                var startInfo = new ProcessStartInfo("/bin/bash", "nodeos -e -p eosio --plugin eosio::wallet_api_plugin --plugin eosio::wallet_plugin --plugin eosio::producer_plugin --plugin eosio::history_plugin --plugin eosio::chain_api_plugin --plugin eosio::history_api_plugin --plugin eosio::http_plugin -d /mnt/dev/data --config-dir /mnt/dev/config --http-server-address=0.0.0.0:8888 --access-control-allow-origin=* --contracts-console --http-validate-host=false");
                startInfo.UseShellExecute = false;
                var process = Process.Start(startInfo);
                process.WaitForExit();
            });
        }

        public async Task WaitEosNodeAsync()
        {
            while(true)
            {
                using (var response = await _client.GetAsync("/"))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        break;
                    }
                }
                await Task.Delay(1000);
            }
        }

        public (string publicKey, string privateKey) RetriveSignatureProviderKey()
        {
            const string configPath = "/config.ini";
            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException(configPath);
            }

            var lines = File.ReadAllLines(configPath);
            var signatureLine = lines.SingleOrDefault(x => x.StartsWith("signature-provider"));
            if (signatureLine == null)
            {
                throw new Exception("Line signature-provider was not found");
            }

            var splitedStrings = signatureLine.Split('=');
            var publicKey = splitedStrings[1].Trim();
            var privateKey = splitedStrings[2].Trim().Substring(4);

            return (publicKey, privateKey);
        }

        public async Task<string> RetriveChainIdAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var response = await _client.GetAsync("/v1/chain/get_info"))
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var getChainInfoResponse = JsonConvert.DeserializeObject<GetChainInfoResponseBody>(jsonString);
                return getChainInfoResponse.chain_id;
            }
        }

        private class GetChainInfoResponseBody
        {
            public string chain_id { get; set; }
        }
    }
}
