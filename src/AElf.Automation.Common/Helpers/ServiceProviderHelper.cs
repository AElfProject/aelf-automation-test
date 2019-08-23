using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AElf.Automation.Common.Helpers
{
    public class ServiceProviderHelper
    {
        private static ServiceCollection _serviceCollection { get; set; }
        private static ServiceProvider _serviceProvider { get; set; }

        public static ServiceCollection InitializeServiceCollection()
        {
            _serviceCollection = new ServiceCollection();
            _serviceCollection.AddLogging();

            return _serviceCollection;
        }
        
        public static ServiceProvider BuildServiceProvider()
        {
            if (_serviceCollection == null)
                throw new InvalidOperationException("Service collection is null.");

            _serviceProvider = _serviceCollection.BuildServiceProvider();

            return _serviceProvider;
        }

        public static ILoggerFactory GetLoggerFactory()
        {
            if (_serviceProvider == null)
                BuildServiceProvider();

            return _serviceProvider.GetService<ILoggerFactory>();
        }

        public static ServiceCollection AddSingleton<TService>()
            where TService : class
        {
            if (_serviceCollection == null)
                InitializeServiceCollection();

            _serviceCollection.AddSingleton<TService>();

            return _serviceCollection;
        }

        public static ServiceCollection AddSingleton<TService, TImplementation>()
            where TService: class
            where TImplementation: class, TService
        {
            if (_serviceCollection == null)
                InitializeServiceCollection();

            _serviceCollection.AddSingleton<TService, TImplementation>();

            return _serviceCollection;
        }
        
        public static ServiceCollection AddTransient<TService>()
            where TService : class
        {
            if (_serviceCollection == null)
                InitializeServiceCollection();

            _serviceCollection.AddTransient<TService>();

            return _serviceCollection;
        }
        
        public static ServiceCollection AddTransient<TService, TImplementation>()
            where TService: class
            where TImplementation: class, TService
        {
            if (_serviceCollection == null)
                InitializeServiceCollection();

            _serviceCollection.AddTransient<TService, TImplementation>();

            return _serviceCollection;
        }
    }
}