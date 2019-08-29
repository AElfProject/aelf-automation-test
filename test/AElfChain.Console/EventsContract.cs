using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Automation.Common.Helpers;
using AElf.Contracts.TestContract.Events;
using AElf.Types;
using AElfChain.AccountService;
using AElfChain.ContractService;
using AElfChain.TestBase;
using Microsoft.Extensions.Logging;
using Shouldly;

namespace AElfChain.Console
{
    public class EventsContract
    {
        public async Task EventContract_Verify()
        {
            ServiceStore.GetServiceProvider<ConsoleModule>();
            var accountManager = ServiceStore.AccountManager;
            var contractManager = ServiceStore.Provider.GetService<ISystemContract>();
            var authorityManager = ServiceStore.Provider.GetService<IAuthorityManager>();
            
            var logger = ServiceStore.LoggerFactory.CreateLogger(nameof(Program));
            var account = await accountManager.GetRandomAccountInfoAsync();
            
            var token = await contractManager.GetSystemContractAddressAsync(SystemContracts.MultiToken);

            var bpAccount = NodeInfoHelper.Config.Nodes.First();
            var bpInfo = await accountManager.GetAccountInfoAsync(bpAccount.Account, bpAccount.Password);

            var eventsContract = await authorityManager.DeployContractWithAuthority(bpInfo, "AElf.Contracts.TestContract.Events");
            var eventStub = contractManager.GetTestStub<EventsContractContainer.EventsContractStub>(eventsContract, bpInfo);
            await eventStub.InitializeEvents.SendAsync(new InitializeInput
            {
                Manager = account.Account
            });
            
            //approve
            var tokenStub = contractManager.GetTestStub<TokenContractContainer.TokenContractStub>(token, bpInfo);
            await tokenStub.Approve.SendAsync(new ApproveInput
            {
                Spender = eventsContract,
                Symbol = "ELF",
                Amount = 1000_000
            });
            
            //issue order
            var transactionResult = await eventStub.IssueOrder.SendAsync(new OrderInput
            {
                SymbolPaid = "ELF",
                SymbolObtain = "NET",
                BalancePaid = 1000_000,
                BalanceObtain = 500_000,
            });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            logger.LogInformation("test complete.");
        }
    }
}