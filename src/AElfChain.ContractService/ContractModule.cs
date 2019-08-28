using AElf.Automation.Common.Helpers;
using AElf.CSharp.Core;
using AElfChain.AccountService;
using AElfChain.SDK;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Modularity;

namespace AElfChain.ContractService
{
    [DependsOn(
        typeof(SdkModule),
        typeof(AccountModule))]
    public class ContractModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            services.TryAddTransient<IMethodStubFactory, MethodStubFactory>();
            services.AddSingleton<IContractFactory, ContractFactory>();
            
            services.AddTransient<IContract, BaseContract>();
            services.AddSingleton<ISystemContract, SystemContract>();

            services.AddSingleton<SmartContractReader>();
            services.AddSingleton<IAuthorityManager, AuthorityManager>();
        }
    }
}