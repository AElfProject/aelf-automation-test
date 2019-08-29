using System.Threading.Tasks;
using AElfChain.TestBase;
using Microsoft.Extensions.Logging;

namespace AElfChain.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            ServiceStore.GetServiceProvider<ConsoleModule>();
            
            var logger = ServiceStore.LoggerFactory.CreateLogger(nameof(Program));
            logger.LogInformation("Start test.");
            
            var token = new TokenIssue();
            await token.PrepareSomeToken("eu6nm4Kxu3HcA7FhSdQpPjy29x896yqcPHSq55gKaggTKEwA3");
            await token.DeployTokenContract();
            await token.GetTokenStub();
            await token.ExecuteTokenTest();
            
            logger.LogInformation("complete test.");
        }
    }
}