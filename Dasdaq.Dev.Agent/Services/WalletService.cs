using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Newtonsoft.Json;
using Dasdaq.Dev.Agent.Models;

namespace Dasdaq.Dev.Agent.Services
{
    public class WalletService
    {
        internal const string _privateKeyFilePath = "/home/dasdaq_eos/wallet/privatekey.txt";
        private EosService _eosService;

        public WalletService(EosService eosService)
        {
            this._eosService = eosService;
        }

        public string GenerateWallet()
        {
            // Start cleos to create a wallet
            var startInfo = new ProcessStartInfo("/opt/eosio/bin/cleos", "-u http://0.0.0.0:8888 --wallet-url http://0.0.0.0:8888 wallet create");
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            var process = Process.Start(startInfo);
            process.WaitForExit();
            var output = process.StandardOutput.ReadToEnd();

            // Find out the private key string
            var regex = new Regex("(?<=\").*?(?=\")");
            var matchResult = regex.Match(output);
            if (!matchResult.Success)
            {
                var error = "Wallet create failed. \r\n Output: \r\n" + output;
                Console.Error.WriteLine(error);
                throw new Exception(error);
            }

            StoreWalletPrivateKey(matchResult.Value);
            ImportPrivateKeys(_eosService.RetriveSignatureProviderKey().privateKey, 
                JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json")).PrivateKey);
            return matchResult.Value;
        }

        public void StoreWalletPrivateKey(string privateKey)
        {
            File.WriteAllText(_privateKeyFilePath, privateKey);
        }

        public string GetPrivateKey()
        {
            if (!File.Exists(_privateKeyFilePath))
            {
                throw new FileNotFoundException(_privateKeyFilePath);
            }

            return File.ReadAllText(_privateKeyFilePath);
        }

        public void UnlockWallet()
        {
            // Start cleos to unlock the wallet
            var startInfo = new ProcessStartInfo("/opt/eosio/bin/cleos", "-u http://0.0.0.0:8888 --wallet-url http://0.0.0.0:8888 wallet unlock --password " + GetPrivateKey());
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            var process = Process.Start(startInfo);
            process.WaitForExit();
            var output = process.StandardOutput.ReadToEnd();
        }

        public void ImportPrivateKeys(params string[] privateKeys)
        {
            foreach(var x in privateKeys)
            {
                ImportPrivateKey(x);
            }
        }

        private void ImportPrivateKey(string privateKey)
        {
            var startInfo = new ProcessStartInfo("/opt/eosio/bin/cleos", "-u http://0.0.0.0:8888 --wallet-url http://0.0.0.0:8888 wallet import --private-key " + privateKey);
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            var process = Process.Start(startInfo);
            process.WaitForExit();
        }
    }
}
