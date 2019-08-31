using System;
using AElfChain.AccountService;
using AElfChain.SDK;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElfChain.TestBase
{
    public static class ServiceContainer
    {
        public static IServiceProvider Provider => GetServiceProvider();

        public static ILoggerFactory LoggerFactory => Provider.GetService<ILoggerFactory>();

        public static IAccountManager AccountManager => Provider.GetService<IAccountManager>();

        public static IApiService GetApiService(string url)
        {
            var apiService = Provider.GetService<IApiService>();
            apiService.SetServiceUrl(url);

            return apiService;
        }

        public static ITransactionManager GetTransactionManager()
        {
            var transactionManager = Provider.GetService<ITransactionManager>();

            return transactionManager;
        }
        
        public static T GetService<T>(this IServiceProvider provider)
            where T : class
        {
            if (provider == null)
                throw new ArgumentNullException(nameof (provider));
            return (T) provider.GetService(typeof (T));
        }
        
        public static IServiceProvider GetServiceProvider<T>()
            where T : AbpModule
        {
            _provider = InitializeModule<T>();

            return _provider;
        }
        
        public static IServiceProvider GetServiceProvider()
        {
            if (_provider != null)
                return _provider;
            
            _provider = InitializeModule<TestBaseModule>();
            return _provider;
        }
        
        private static IServiceProvider _provider;
        
        private static IServiceProvider InitializeModule<T>()
            where T : AbpModule
        {
            var application = AbpApplicationFactory.Create<T>(option =>
            {
                option.UseAutofac();
            });
            application.Initialize();
            
            return application.ServiceProvider;
        }
    }
}