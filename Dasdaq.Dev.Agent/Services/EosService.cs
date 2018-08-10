using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Dasdaq.Dev.Agent.Models;

namespace Dasdaq.Dev.Agent.Services
{
    public class EosService : IDisposable
    {
        internal const string eosioToken = "eosio.token";
        internal const string _dasdaqRootPath = "/home/dasdaq_eos";
        internal const string _instancesPath = "/home/dasdaq_eos/instances";
        internal const string _privateKeyFilePath = "/home/dasdaq_eos/wallet/privatekey.txt";
        internal const string _contractsFolderPath = "/home/dasdaq_eos/contracts";
        internal const string _tempFolderPath = "/home/dasdaq_eos/temp";

        private static HttpClient _client = new HttpClient() { BaseAddress = new Uri("http://127.0.0.1:8888") };
        private AgentContext _ef;

        public EosService(AgentContext ef)
        {
            if (!Directory.Exists(_dasdaqRootPath))
            {
                Directory.CreateDirectory(_dasdaqRootPath);
            }
            if (!Directory.Exists(_instancesPath))
            {
                Directory.CreateDirectory(_instancesPath);
            }
            if (!Directory.Exists(_contractsFolderPath))
            {
                Directory.CreateDirectory(_contractsFolderPath);
            }
            if (!Directory.Exists(_tempFolderPath))
            {
                Directory.CreateDirectory(_tempFolderPath);
            }

            _ef = ef;
        }
        
        public void Launch()
        {
            try
            {
                if (Process.GetProcessesByName("nodeos").Length > 0)
                {
                    return;
                }
                EnsureRemoveDefaultWallet();
                StartEosNodeAsync().ConfigureAwait(false);
                WaitEosNodeAsync().Wait();
                GenerateWallet();
                DeployEosioToken();
                DownloadAndDeployContracts();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Dasdaq Dev Agent] An error occurred while launching EOS: \r\n" + ex.ToString());
            }
        }

        public void CreateAccount(string name)
        {
            // Start cleos to unlock the wallet
            var publicKey = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json")).PublicKey;
            var startInfo = new ProcessStartInfo("/opt/eosio/bin/cleos", $"-u http://0.0.0.0:8888 --wallet-url http://0.0.0.0:8888 create account eosio {name} {publicKey} {publicKey}");
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            var process = Process.Start(startInfo);
            process.WaitForExit();
            _ef.Accounts.Add(new Account
            {
                Name = name
            });
            _ef.SaveChanges();
        }
        
        public void CreateCurrency(string currency, string account, double amount)
        {
            InvokeContract(eosioToken, "create", eosioToken, account, $"{amount.ToString("0.0000")} {currency}");
            _ef.Currencies.Add(new Currency
            {
                Name = currency,
                BaseAccount = account
            });
            _ef.SaveChanges();
        }

        public void IssueCurrency(string currency, string account, double amount)
        {
            var cur = _ef.Currencies.SingleOrDefault(x => x.Name == currency);
            if (cur == null)
            {
                throw new KeyNotFoundException(currency);
            }
            InvokeContract(eosioToken, "issue", cur.BaseAccount, account, $"{amount.ToString("0.0000")} {currency}", "");
        }
        
        public void DownloadAndDeployContracts()
        {
            var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));

            // Start git to clone smart contracts
            CleanUpContractFolder();
            CloneContractRepo();

            var contracts = Directory.EnumerateDirectories(Path.Combine(_tempFolderPath, config.ContractsPath));
            Console.WriteLine($"[Dasdaq Dev Agent] Downloaded {contracts.Count()} contracts.");
            var pendingPublishContracts = new List<string>();
            foreach (var x in contracts)
            {
                var name = Path.GetFileName(x);
                if (File.Exists(Path.Combine(x, name + ".cpp")))
                {
                    var cpp = File.ReadAllText(Path.Combine(x, name + ".cpp"));
                    string hpp = null;
                    if (File.Exists(Path.Combine(x, name + ".hpp")))
                    {
                        hpp = File.ReadAllText(Path.Combine(x, name + ".hpp"));
                    }
                    SaveContract(name, cpp, hpp);
                    pendingPublishContracts.Add(name);
                }
            }

            foreach(var x in pendingPublishContracts)
            {
                CompileAndPublishContract(x);
            }
        }

        public void CompileAndPublishContract(string name)
        {
            CompileContract(name);
            PublishContract(name);
        }

        public void InvokeContract(string contractAccount, string method, string invokerAccount, params object[] args)
        {
            // Start cleos to invoke a smart contract
            Console.WriteLine($"[Dasdaq Dev Agent] Invoking {contractAccount} {method}.");
            var argsJson = JsonConvert.SerializeObject(args);
            var contractFolder = ConcatPath(contractAccount);
            var startInfo = new ProcessStartInfo("bash");
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardInput = true;
            var process = Process.Start(startInfo);
            process.StandardInput.WriteLine($"cleos -u http://0.0.0.0:8888 --wallet-url http://0.0.0.0:8888 push action {contractAccount} {method} '{argsJson}' -p {invokerAccount}");
            process.StandardInput.Close();
            process.WaitForExit();
        }

        public void SaveContract(string name, string cpp, string hpp = null)
        {
            Contract contract;
            var isCreate = false;
            if (_ef.Contracts.Any(x => x.Name == name))
            {
                isCreate = false;
                contract = _ef.Contracts.Single(x => x.Name == name);
            }
            else
            {
                isCreate = true;
                contract = new Contract();
                contract.Name = name;
            }

            contract.Cpp = cpp;
            contract.Hpp = hpp;
            contract.DeployedTime = DateTime.Now;
            contract.Status = ContractStatus.Updating;
            if (isCreate)
            {
                _ef.Add(contract);
            }
            _ef.SaveChanges();

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
        }

        public Task StartEosNodeAsync()
        {
            return Task.Factory.StartNew(()=> {
                // Start bash to launch nodeos
                var startInfo = new ProcessStartInfo("/opt/eosio/bin/nodeos", "-e -p eosio --plugin eosio::wallet_api_plugin --plugin eosio::wallet_plugin --plugin eosio::producer_plugin --plugin eosio::history_plugin --plugin eosio::chain_api_plugin --plugin eosio::history_api_plugin --plugin eosio::http_plugin -d /mnt/dev/data --config-dir /mnt/dev/config --http-server-address=0.0.0.0:8888 --access-control-allow-origin=* --contracts-console --http-validate-host=false --delete-all-blocks");
                startInfo.UseShellExecute = false;
                var process = Process.Start(startInfo);
                process.WaitForExit();
            });
        }

        public async Task WaitEosNodeAsync()
        {
            while(true)
            {
                try
                {
                    using (var response = await _client.GetAsync("/"))
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            break;
                        }
                    }
                }
                catch
                {
                }
                finally
                {
                    await Task.Delay(1000);
                }
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
            ImportPrivateKeys(RetriveSignatureProviderKey().privateKey,
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
            foreach (var x in privateKeys)
            {
                ImportPrivateKey(x);
            }
        }

        private void CloneContractRepo()
        {
            // Start git to clone smart contracts
            var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));
            Console.WriteLine($"[Dasdaq Dev Agent] Cloning contracts repo: {config.Contracts}.");
            var startInfo = new ProcessStartInfo("git", $"clone {config.Contracts}");
            startInfo.UseShellExecute = false;
            startInfo.WorkingDirectory = _tempFolderPath;
            var process = Process.Start(startInfo);
            process.WaitForExit();
        }

        private void CleanUpContractFolder()
        {
            var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));
            var endpoint = config.Contracts.TrimEnd('/');
            var folder = endpoint.Substring(endpoint.LastIndexOf('/') + 1);
            var path = Path.Combine(_tempFolderPath, folder);
            Directory.Delete(path, true);
        }

        private void DeployEosioToken()
        {
            const string eosioToken = "eosio.token";
            SaveContract(eosioToken, File.ReadAllText($"Token/{eosioToken}.cpp"), File.ReadAllText($"Token/{eosioToken}.hpp"));
            CompileAndPublishContract(eosioToken);
        }
        
        private class GetChainInfoResponseBody
        {
            public string chain_id { get; set; }
        }

        private void EnsureRemoveDefaultWallet()
        {
            // Start cleos to unlock the wallet
            var walletPath = "/mnt/dev/data/default.wallet";
            var startInfo = new ProcessStartInfo("rm", "-rf " + walletPath);
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            var process = Process.Start(startInfo);
            process.WaitForExit();
            var output = process.StandardOutput.ReadToEnd();
        }

        private void PublishContract(string name)
        {
            Console.WriteLine($"[Dasdaq Dev Agent] Publishing contract {name}.");
            var contractFolder = ConcatPath(name);
            UnlockWallet();
            CreateAccount(name);
            SetContractToAccount(name);
            var contract = _ef.Contracts.Single(x => x.Name == name);
            contract.Status = ContractStatus.Available;
            _ef.SaveChanges();
        }

        private string ConcatPath(string name)
        {
            return Path.Combine(_contractsFolderPath, name);
        }

        private void CompileContract(string name)
        {
            Console.WriteLine($"[Dasdaq Dev Agent] Compiling {name}.cpp");
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

        private void ImportPrivateKey(string privateKey)
        {
            var startInfo = new ProcessStartInfo("/opt/eosio/bin/cleos", "-u http://0.0.0.0:8888 --wallet-url http://0.0.0.0:8888 wallet import --private-key " + privateKey);
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            var process = Process.Start(startInfo);
            process.WaitForExit();
        }

        public void Dispose()
        {
            this._ef.Dispose();
        }
    }
}
