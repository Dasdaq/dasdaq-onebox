using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Dasdaq.Dev.Agent.Hubs;
using Dasdaq.Dev.Agent.Models;

namespace Dasdaq.Dev.Agent.Services
{
    public class EosService : IDisposable
    {
        internal const string _eosioToken = "eosio.token";
        internal const string _dasdaqRootPath = "/home/dasdaq_eos";
        internal const string _instancesPath = "/home/dasdaq_eos/instances";
        internal const string _privateKeyFilePath = "/home/dasdaq_eos/wallet/privatekey.txt";
        internal const string _contractsFolderPath = "/home/dasdaq_eos/contracts";
        internal const string _tempFolderPath = "/home/dasdaq_eos/temp";
        internal const string _walletPath = "/mnt/dev/data/default.wallet";

        private static HttpClient _client = new HttpClient() { BaseAddress = new Uri("http://127.0.0.1:8888") };
        private static OneBoxProcess _oneboxProc;
        private AgentContext _ef;
        private ProcessService _proc;
        private IHubContext<AgentHub> _hub;

        public EosService(AgentContext ef, ProcessService proc, IHubContext<AgentHub> hub)
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
            _proc = proc;
            _hub = hub;
        }

        public Guid? GetOneBoxProcId()
        {
            if (_oneboxProc != null)
            {
                return _oneboxProc.Id;
            }
            else
            {
                return null;
            }
        }

        public bool Launch(bool safeMode = false)
        {
            try
            {
                if (Process.GetProcessesByName("nodeos").Length > 0)
                {
                    return false;
                }
                if (safeMode)
                {
                    Console.WriteLine("[Dasdaq Dev Agent] Starting in safe mode, force removing existed wallet.");
                    EnsureRemoveDefaultWallet();
                }
                var id = StartEosNode();

                Console.WriteLine("[Dasdaq Dev Agent] Starting EOS node, OneBox Proc Id = " + id);
                WaitEosNodeAsync().Wait();
                Console.WriteLine("[Dasdaq Dev Agent] EOS node web API is ready.");
                if (!File.Exists(_walletPath))
                {
                    Console.WriteLine("[Dasdaq Dev Agent] Wallet is not found, generating...");
                    GenerateWallet();
                }
                if (!DeployEosioToken())
                {
                    Console.WriteLine("[Dasdaq Dev Agent] Deploy eosio.token failed...");
                    ForceShutdown();
                    return false;
                }
                if (!DownloadAndDeployContracts())
                {
                    Console.WriteLine("[Dasdaq Dev Agent] (warn) Preinstall contracts failed...");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Dasdaq Dev Agent] An error occurred while launching EOS: \r\n" + ex.ToString());
                return false;
            }

            return true;
        }

        public void GracefulShutdown()
        {
            ExecuteCommand("kill -15 " + _oneboxProc.Process.Id);
            _oneboxProc = null;
        }

        public void ForceShutdown()
        {
            ExecuteCommand("kill -15 " + _oneboxProc.Process.Id);
            _oneboxProc = null;
        }

        public CommandResult ExecuteCommand(string command, string workDir = null)
        {
            var startInfo = new ProcessStartInfo("bash");
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardInput = true;
            if (workDir != null)
            {
                startInfo.WorkingDirectory = workDir;
            }
            var process = Process.Start(startInfo);
            process.StandardInput.WriteLine(command);
            process.StandardInput.Close();
            process.WaitForExit();
            var result = new CommandResult
            {
                ErrorOutput = process.StandardError.ReadToEnd(),
                StandardOutput = process.StandardOutput.ReadToEnd(),
                ExitCode = process.ExitCode,
                IsSucceeded = process.ExitCode == 0
            };

            if (!string.IsNullOrEmpty(result.ErrorOutput))
            {
                PushCleosLogsToEosChannel(result.ErrorOutput, true, process.Id);
            }

            if (!string.IsNullOrEmpty(result.StandardOutput))
            {
                PushCleosLogsToEosChannel(result.StandardOutput, false, process.Id);
            }

            return result;
        }

        public CommandResult ExecuteCleosCommand(string command)
        {
            return ExecuteCommand($"cleos -u http://0.0.0.0:8888 --wallet-url http://0.0.0.0:8888 {command}");
        }

        public CommandResult ExecuteEosioCppCommand(string command, string workDir)
        {
            return ExecuteCommand($"eosiocpp {command}", workDir);
        }

        public CommandResult ExecuteGitCommand(string command, string workDir)
        {
            return ExecuteCommand($"git {command}", workDir);
        }

        public bool CreateAccount(string name)
        {
            // Start cleos to unlock the wallet
            var publicKey = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json")).eos.keyPair.publicKey;
            var result = ExecuteCleosCommand($"create account eosio {name} {publicKey} {publicKey}");

            if (!result.IsSucceeded)
            {
                Console.WriteLine("[Dasdaq Dev Agent] Create account process returned " + result.ExitCode);
                return false;
            }

            _ef.Accounts.Add(new Account
            {
                Name = name
            });
            _ef.SaveChanges();
            return true;
        }
        
        public bool CreateCurrency(string currency, string account, double amount)
        {
            var isSucceeded = InvokeContract(_eosioToken, "create", _eosioToken, account, $"{amount.ToString("0.0000")} {currency}");
            if (!isSucceeded)
            {
                return false;
            }

            _ef.Currencies.Add(new Currency
            {
                Name = currency,
                BaseAccount = account
            });
            _ef.SaveChanges();
            return true;
        }

        public bool IssueCurrency(string currency, string account, double amount)
        {
            var cur = _ef.Currencies.SingleOrDefault(x => x.Name == currency);
            if (cur == null)
            {
                Console.WriteLine("[Dasdaq Dev Agent] Currency has not been found.");
                return false;
            }

            return InvokeContract(_eosioToken, "issue", cur.BaseAccount, account, $"{amount.ToString("0.0000")} {currency}", "");
        }
        
        public bool DownloadAndDeployContracts()
        {
            var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));

            // Start git to clone smart contracts
            CleanUpContractFolder();
            if (!CloneContractRepo())
            {
                Console.WriteLine($"[Dasdaq Dev Agent] Clone contract repo failed.");
                return false;
            }

            var endpoint = config.eos.contracts.git.TrimEnd('/');
            var folder = endpoint.Substring(endpoint.LastIndexOf('/') + 1);
            var contracts = Directory.EnumerateDirectories(Path.Combine(_tempFolderPath, folder, config.eos.contracts.folder));
            Console.WriteLine($"[Dasdaq Dev Agent] Downloaded {contracts.Count()} contracts.");
            var pendingPublishContracts = new List<string>();
            foreach (var x in contracts)
            {
                var name = Path.GetFileName(x);
                if (name == "eosio.token")
                {
                    continue;
                }
                
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
            return true;
        }

        public bool CompileAndPublishContract(string name)
        {
            if (!CompileContract(name).IsSucceeded)
            {
                var contract = _ef.Contracts.Single(x => x.Name == name);
                contract.Status = ContractStatus.Failed;
                return false;
            }
            return PublishContract(name);
        }

        public bool InvokeContract(string contractAccount, string method, string invokerAccount, params object[] args)
        {
            // Start cleos to invoke a smart contract
            Console.WriteLine($"[Dasdaq Dev Agent] Invoking {contractAccount} {method}.");
            var argsJson = JsonConvert.SerializeObject(args);
            var contractFolder = ConcatPath(contractAccount);
            var result = ExecuteCleosCommand($"push action {contractAccount} {method} '{argsJson}' -p {invokerAccount}");
            return result.IsSucceeded;
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

        public Guid StartEosNode()
        {
            var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));
            var pluginsCommand = string.Join(' ', config.eos.plugins.Select(x => $"--plugin {x}"));
            var startInfo = new ProcessStartInfo("/opt/eosio/bin/nodeos", $"-e -p eosio {pluginsCommand} -d /mnt/dev/data --config-dir /mnt/dev/config --http-server-address=0.0.0.0:8888 --access-control-allow-origin=* --contracts-console --http-validate-host=false --delete-all-blocks");
            _oneboxProc = _proc.StartProcess(startInfo, async (id, x) => {
                try
                {
                    await _hub.Clients.All.SendAsync("onLogReceived", id, x.IsError, x.Text);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[Dasdaq Dev Agent] " + ex.ToString());
                }
            }, "nodeos");
            Task.Factory.StartNew(()=> {
                // Start bash to launch nodeos
                _oneboxProc.Process.Start();
                _oneboxProc.Process.WaitForExit();
            }).ConfigureAwait(false);
            return _oneboxProc.Id;
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
            var result = ExecuteCleosCommand("wallet create");
            var output = result.StandardOutput;

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
                JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json")).eos.keyPair.privateKey);
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

        public bool UnlockWallet()
        {
            // Start cleos to unlock the wallet
            return ExecuteCleosCommand($"wallet unlock --password {GetPrivateKey()}").IsSucceeded;
        }

        public void ImportPrivateKeys(params string[] privateKeys)
        {
            foreach (var x in privateKeys)
            {
                ImportPrivateKey(x);
            }
        }
        
        private void PushCleosLogsToEosChannel(string text, bool isError, int processId)
        {
            if (_oneboxProc == null)
            {
                return;
            }

            _oneboxProc.Logs.Add(new Log
            {
                IsError = isError,
                ProcessId = processId,
                Text = text,
                Time = DateTime.Now
            });

            try
            {
                _hub.Clients.All.SendAsync("onLogReceived", _oneboxProc.Id, isError, text);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Dasdaq Dev Agent] " + ex.ToString());
            }
        }

        private bool CloneContractRepo()
        {
            // Start git to clone smart contracts
            var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));
            Console.WriteLine($"[Dasdaq Dev Agent] Cloning contracts repo: {config.eos.contracts.git}.");
            var result = ExecuteGitCommand($"clone {config.eos.contracts.git}", _tempFolderPath);
            return result.IsSucceeded;
        }

        private void CleanUpContractFolder()
        {
            var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("config.json"));
            var endpoint = config.eos.contracts.git.TrimEnd('/');
            var folder = endpoint.Substring(endpoint.LastIndexOf('/') + 1);
            var path = Path.Combine(_tempFolderPath, folder);
            try
            {
                Directory.Delete(path, true);
            }
            catch (DirectoryNotFoundException) { }
        }

        private bool DeployEosioToken()
        {
            const string eosioToken = "eosio.token";
            SaveContract(eosioToken, File.ReadAllText($"Token/{eosioToken}.cpp"), File.ReadAllText($"Token/{eosioToken}.hpp"));
            return CompileAndPublishContract(eosioToken);
        }
        
        private class GetChainInfoResponseBody
        {
            public string chain_id { get; set; }
        }

        private bool EnsureRemoveDefaultWallet()
        {
            // Start cleos to unlock the wallet
            var walletPath = _walletPath;
            var result = ExecuteCommand("rm -rf " + walletPath);
            return result.IsSucceeded;
        }

        private bool PublishContract(string name)
        {
            Console.WriteLine($"[Dasdaq Dev Agent] Publishing contract {name}.");
            var contractFolder = ConcatPath(name);
            UnlockWallet();
            if (!_ef.Accounts.Any(x => x.Name == name) && !CreateAccount(name))
            {
                Console.WriteLine($"[Dasdaq Dev Agent] Create contract account failed.");
                return false;
            }
            if (!MapContractToAccount(name))
            {
                Console.WriteLine($"[Dasdaq Dev Agent] Map contract to account failed.");
                return false;
            }
            var contract = _ef.Contracts.Single(x => x.Name == name);
            contract.Status = ContractStatus.Available;
            _ef.SaveChanges();
            return true;
        }

        private string ConcatPath(string name)
        {
            return Path.Combine(_contractsFolderPath, name);
        }

        private ContractCompileResult CompileContract(string name)
        {
            Console.WriteLine($"[Dasdaq Dev Agent] Compiling {name}.cpp");
            var wastResult = CompileContractWast(name);
            var abiResult = CompileContractAbi(name);
            return new ContractCompileResult
            {
                IsSucceeded = wastResult.IsSucceeded && abiResult.IsSucceeded,
                ErrorMessage = wastResult.ErrorMessage + abiResult.ErrorMessage
            };
        }

        private ContractCompileResult CompileContractWast(string name)
        {
            // Start eosiocpp to compile smart contract
            var result = ExecuteEosioCppCommand($"-o {name + ".wast"} {name + ".cpp"}", Path.Combine(_contractsFolderPath, name));
            return new ContractCompileResult
            {
                IsSucceeded = result.IsSucceeded,
                ErrorMessage = result.ErrorOutput
            };
        }

        private ContractCompileResult CompileContractAbi(string name)
        {
            // Start eosiocpp to compile smart contract
            var contractFolder = ConcatPath(name);
            var result = ExecuteEosioCppCommand($"-g {name + ".abi"} {name + ".cpp"}", Path.Combine(_contractsFolderPath, name));
            return new ContractCompileResult
            {
                IsSucceeded = result.IsSucceeded,
                ErrorMessage = result.ErrorOutput
            };
        }

        private bool MapContractToAccount(string name)
        {
            // Start cleos to map contract with account
            var contractFolder = ConcatPath(name);
            var result = ExecuteCleosCommand($"set contract {name} {Path.Combine(contractFolder)} -p {name}@active");
            return result.IsSucceeded;
        }

        private bool ImportPrivateKey(string privateKey)
        {
            var result = ExecuteCleosCommand("wallet import --private-key " + privateKey);
            return result.IsSucceeded;
        }

        public void Dispose()
        {
            this._ef.Dispose();
        }
    }
}
