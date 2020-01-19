using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Configuration;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class AcsCodeCheck
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        public INodeManager NodeManager { get; set; }
        public ContractManager ContractManager { get; set; }

        public AcsCodeCheck()
        {
            Log4NetHelper.LogInit();
            NodeInfoHelper.SetConfig("nodes-env2-side1");
            var firstNode = NodeInfoHelper.Config.Nodes.First();

            NodeManager = new NodeManager(firstNode.Endpoint);
            ContractManager = new ContractManager(NodeManager, firstNode.Account);
        }

        [TestMethod]
        public async Task QueryRequiredAcsContracts()
        {
            var acsResult = await ContractManager.ConfigurationStub.GetRequiredAcsInContracts.CallAsync(new Empty());
            Logger.Info($"{JsonConvert.SerializeObject(acsResult)}");
        }

        [TestMethod]
        public async Task SetEnableRequireAllAcs()
        {
            var transactionResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.Configuration.ContractAddress,
                nameof(ContractManager.ConfigurationStub.SetRequiredAcsInContracts),
                new RequiredAcsInContracts
                {
                    AcsList = {"acs1", "acs8"},
                    RequireAll = false
                }, ContractManager.CallAddress);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            await QueryRequiredAcsContracts();
        }
    }
}