using AElfChain.AccountService;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElfChain.ContractService
{
    [DependsOn(typeof(AccountModule))]
    public class ContractModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            services.AddTransient<IContract, BaseContract>();
            services.AddSingleton<ISystemContract, SystemContract>();
        }
    }
}