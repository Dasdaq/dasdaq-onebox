using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using Dasdaq.Dev.Agent.Models;

namespace Dasdaq.Dev.Agent.Services
{
    public class ContractService
    {
        internal const string _contractsFolderPath = "/home/dasdaq_eos/contracts";
        internal const string _tempFolderPath = "/home/dasdaq_eos/temp";
        private WalletService _walletService;
        private AccountService _accountService;

        public ContractService(WalletService walletService)
        {
            _walletService = walletService;
        }

        public void InitializeEosioToken()
        {
            UploadContract("eosio.token", File.ReadAllText("Token/eosio.token.cpp"), File.ReadAllText("Token/eosio.token.hpp"));
            InvokeContract("eosio.token", "create", "eosio", "1000000000.0000 SYS");
        }

        public void DownloadAndDeployContracts()
        {
            // Start git to clone smart contracts
            var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));
            var startInfo = new ProcessStartInfo("git", $"clone {config.Contracts}");
            startInfo.UseShellExecute = false;
            startInfo.WorkingDirectory = _tempFolderPath;
            var process = Process.Start(startInfo);
            process.WaitForExit();
            var contracts = Directory.EnumerateDirectories(Path.Combine(_tempFolderPath, config.Contracts));
            foreach(var x in contracts)
            {
                var name = Path.GetDirectoryName(x);
                if (File.Exists(Path.Combine(x, name + ".cpp")))
                {
                    var cpp = File.ReadAllText(Path.Combine(x, name + ".cpp"));
                    string hpp = null;
                    if (File.Exists(Path.Combine(x, name + ".hpp")))
                    {
                        hpp = File.ReadAllText(Path.Combine(x, name + ".hpp"));
                    }
                    UploadContract(name, cpp, hpp);
                }
            }
        }

        public void InvokeContract(string name, string method, params object[] args)
        {
            // Start cleos to invoke a smart contract
            var argsJson = JsonConvert.SerializeObject(args);
            var contractFolder = ConcatPath(name);
            var startInfo = new ProcessStartInfo("/opt/eosio/bin/cleos", $"-u http://0.0.0.0:8888 --wallet-url http://0.0.0.0:8888 push action {method} '{argsJson}' -p {name}@active");
            startInfo.UseShellExecute = false;
            var process = Process.Start(startInfo);
            process.WaitForExit();
        }

        public void UploadContract(string name, string cpp, string hpp = null)
        {
            var contractFolder = ConcatPath(name);
            if (!Directory.Exists(contractFolder))
            {
                Directory.CreateDirectory(contractFolder);
            }

            File.WriteAllText(Path.Combine(contractFolder, name + ".cpp"), cpp);
            if (hpp != null)
            {
                File.WriteAllText(Path.Combine(contractFolder, name + ".hpp"), hpp);
            }

            CompileContract(name);
            PublishContract(name);
        }

        private void PublishContract(string name)
        {
            var contractFolder = ConcatPath(name);
            _walletService.UnlockWallet();
            _accountService.CreateAccount(name);
            SetContractToAccount(name);
        }

        private string ConcatPath(string name)
        {
            return Path.Combine(_contractsFolderPath, name);
        }

        private void CompileContract(string name)
        {
            CompileContractWast(name);
            CompileContractAbi(name);
        }

        private void CompileContractWast(string name)
        {
            // Start eosiocpp to compile smart contract
            var contractFolder = ConcatPath(name);
            var startInfo = new ProcessStartInfo("/opt/eosio/bin/eosiocpp", $"-o {Path.Combine(contractFolder, name + ".wast")} {Path.Combine(contractFolder, name + ".cpp")}");
            startInfo.UseShellExecute = false;
            var process = Process.Start(startInfo);
            process.WaitForExit();
        }

        private void CompileContractAbi(string name)
        {
            // Start eosiocpp to compile smart contract
            var contractFolder = ConcatPath(name);
            var startInfo = new ProcessStartInfo("/opt/eosio/bin/eosiocpp", $"-g {Path.Combine(contractFolder, name + ".abi")} {Path.Combine(contractFolder, name + ".cpp")}");
            startInfo.UseShellExecute = false;
            var process = Process.Start(startInfo);
            process.WaitForExit();
        }

        private void SetContractToAccount(string name)
        {
            // Start cleos to map contract with account
            var contractFolder = ConcatPath(name);
            var startInfo = new ProcessStartInfo("/opt/eosio/bin/cleos", $"-u http://0.0.0.0:8888 --wallet-url http://0.0.0.0:8888 set contract {name} {Path.Combine(contractFolder)} -p {name}@active");
            startInfo.UseShellExecute = false;
            var process = Process.Start(startInfo);
            process.WaitForExit();
        }
    }
}
