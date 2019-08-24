using AElfChain.AccountService;
using AElfChain.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElfChain.Console
{
    [DependsOn(typeof(TestBaseModule))]
    public class ConsoleModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
}