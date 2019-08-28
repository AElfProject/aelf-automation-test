using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace AElfChain.SDK
{
    [DependsOn(typeof(AbpAutofacModule))]
    public class SdkModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            
            Configure<SdkOption>(o=>o.ServiceUrl = "");
            
            services.AddSingleton<IHttpService, HttpService>();
            services.AddTransient<IApiService, ApiService>();
        }
    }
}