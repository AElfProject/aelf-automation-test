using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Automation.Common.Helpers
{
    public static class AbpHelper
    {
        public static IServiceProvider InitializeModule<T>()
            where T : AbpModule
        {
            var application = AbpApplicationFactory.Create<T>(option =>
            {
                option.UseAutofac();
            });
            application.Initialize();
            
            return application.ServiceProvider;
        }

        public static T GetService<T>(this IServiceProvider provider)
            where T : class
        {
            if (provider == null)
                throw new ArgumentNullException(nameof (provider));
            return (T) provider.GetService(typeof (T));
        }
    }
}