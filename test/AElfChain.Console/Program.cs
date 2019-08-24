using System.Threading.Tasks;
using AElf.Automation.Common.Helpers;
using AElfChain.AccountService;
using AElfChain.TestBase;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AElfChain.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var logger = ServiceStore.LoggerFactory.CreateLogger(nameof(Program));
            logger.LogInformation("start test");

            var accountManager = ServiceStore.AccountManager;
            var account = await accountManager.NewAccountAsync();
            logger.LogInformation(JsonConvert.SerializeObject(account));
        }
    }
}