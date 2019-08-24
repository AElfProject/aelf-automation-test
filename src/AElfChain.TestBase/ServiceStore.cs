using System;
using AElf.Automation.Common.Helpers;
using AElfChain.AccountService;
using AElfChain.SDK;
using Microsoft.Extensions.Logging;

namespace AElfChain.TestBase
{
    public static class ServiceStore
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

        private static IServiceProvider _provider;
        private static IServiceProvider GetServiceProvider()
        {
            if (_provider != null)
                return _provider;
            
            _provider = AbpHelper.InitializeModule<TestBaseModule>();
            return _provider;
        }
    }
}