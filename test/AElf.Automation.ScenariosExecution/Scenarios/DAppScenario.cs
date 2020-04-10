using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestContract.DApp;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using Shouldly;
using Volo.Abp.Threading;

namespace AElf.Automation.ScenariosExecution.Scenarios
{
    public class DAppScenario : BaseScenario
    {
        public new static readonly ILog Logger = Log4NetHelper.GetLogger();

        public DAppScenario()
        {
            InitializeScenario();
            MinerAccounts = ContractServices.CurrentBpNodes.Select(o => o.Account).ToList();
            ContractDeveloper = MinerAccounts.First();
            InitializeDAppContract();
            Testers = AllTesters.GetRange(100, 10);
        }

        public DAppContainer.DAppStub DAppStub { get; set; }
        public DAppContract DAppContract { get; set; }

        public List<string> MinerAccounts { get; set; }
        public List<string> Testers { get; set; }
        public string ContractDeveloper { get; set; }

        private void InitializeDAppContract()
        {
            DAppContract = new DAppContract(Services.NodeManager, ContractDeveloper);
            DAppStub = DAppContract.GetTestStub<DAppContainer.DAppStub>(ContractDeveloper);

            while (true)
            {
                var symbol = CommonHelper.RandomString(4, false);
                var tokenInfo = Services.NodeManager.GetTokenInfo(symbol);
                if (!tokenInfo.Equals(new TokenInfo())) continue;

                var transactionResult = AsyncHelper.RunSync(() => DAppStub.Initialize.SendAsync(new InitializeInput
                {
                    ProfitReceiver = ContractDeveloper.ConvertAddress(),
                    Symbol = symbol
                }));
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined,
                    transactionResult.TransactionResult.Error);
                return;
            }
        }
    }
}