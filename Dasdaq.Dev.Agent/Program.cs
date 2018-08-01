using System;
using Microsoft.Extensions.DependencyInjection;
using Dasdaq.Dev.Agent.Services;

namespace Dasdaq.Dev.Agent
{
    class Program
    {
        static void Main(string[] args)
        {
            var collection = new ServiceCollection();
            collection.AddAgentServices();
            var provider = collection.BuildServiceProvider();

            var eos = provider.GetRequiredService<EosService>();
            eos.StartEosNodeAsync().ConfigureAwait(false);
            eos.WaitEosNodeAsync().Wait();

            var contract = provider.GetRequiredService<ContractService>();
            contract.InitializeEosioToken();
            contract.DownloadAndDeployContracts();

            while(true)
            {
                Console.Read();
            }
        }
    }
}
