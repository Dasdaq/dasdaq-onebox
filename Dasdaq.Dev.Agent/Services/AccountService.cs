using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json;
using Dasdaq.Dev.Agent.Models;

namespace Dasdaq.Dev.Agent.Services
{
    public class AccountService
    {
        public void CreateAccount(string name)
        {
            // Start cleos to unlock the wallet
            var privateKey = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json")).PrivateKey;
            var startInfo = new ProcessStartInfo("/opt/eosio/bin/cleos", $"-u http://127.0.0.1:8888 --wallet-url http://127.0.0.1:8888 create account eosio {name} {privateKey} {privateKey}");
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            var process = Process.Start(startInfo);
            process.WaitForExit();
        }
    }
}
