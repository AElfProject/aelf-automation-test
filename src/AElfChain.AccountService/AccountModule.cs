using AElf.Automation.Common.Helpers;
using AElfChain.AccountService.KeyAccount;
using AElfChain.SDK;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElfChain.AccountService
{
    [DependsOn(typeof(SdkModule))]
    public class AccountModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            
            var dataPath = CommonHelper.MapPath("aelf");
            services.Configure<AccountOption>(o=>o.DataPath = dataPath);

            services.AddSingleton<IKeyStore, AElfKeyStore>();
            services.AddSingleton<IAccountManager, AccountManager>();
            services.AddTransient<ITransactionManager, TransactionManager>();
        }
    }
}