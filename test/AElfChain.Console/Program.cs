using System.Threading.Tasks;
using AElfChain.TestBase;
using Microsoft.Extensions.Logging;

namespace AElfChain.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            ServiceContainer.GetServiceProvider<ConsoleModule>();
            
            var logger = ServiceContainer.LoggerFactory.CreateLogger(nameof(Program));
            logger.LogInformation("Start test.");
            
            var eventsContract = new EventsContract();
            await eventsContract.EventContract_Verify();
            
            logger.LogInformation("complete test.");
        }
    }
}