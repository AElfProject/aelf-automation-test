using apache.log4net.Extensions.Logging;
using AElf.Automation.Common.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.Modularity;

namespace AElfChain.TestBase
{
    public class TestBaseModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var log4NetConfigFile = CommonHelper.MapPath("log4net.config");
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