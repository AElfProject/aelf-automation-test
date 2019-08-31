using AElfChain.SDK;
using AElfChain.TestBase;
using Volo.Abp.Modularity;

namespace AElfChain.Console
{
    [DependsOn(typeof(TestBaseModule))]
    public class ConsoleModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<SdkOption>(o =>
            {
                o.ServiceUrl = "192.168.197.15:8100";
                o.TimeoutSeconds = 60;
                o.FailReTryTimes = 1;
            });
        }
    }
}