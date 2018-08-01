using Microsoft.Extensions.DependencyInjection;

namespace Dasdaq.Dev.Agent.Services
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddAgentServices(this IServiceCollection self)
        {
            return self.AddSingleton<AccountService>()
                .AddSingleton<EosService>()
                .AddSingleton<WalletService>()
                .AddSingleton<ContractService>();
        }
    }
}
