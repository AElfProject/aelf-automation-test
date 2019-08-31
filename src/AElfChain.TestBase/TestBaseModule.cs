using apache.log4net.Extensions.Logging;
using AElf.Automation.Common.Helpers;
using AElfChain.AccountService;
using AElfChain.ContractService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.Modularity;
using log4net;

namespace AElfChain.TestBase
{
    [DependsOn(
        typeof(AccountModule),
        typeof(ContractModule)
    )]
    public class TestBaseModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            //init log4net
            var log4NetConfigFile = CommonHelper.MapPath("log4net.config");
            GlobalContext.Properties["LogName"] = "";
            context.Services.AddLogging(builder =>
            {
                builder.AddLog4Net(new Log4NetSettings()
                {
                    ConfigFile = log4NetConfigFile
                });

                builder.SetMinimumLevel(LogLevel.Debug);
            });
        }
    }
}