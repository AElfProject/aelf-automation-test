using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Configuration;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
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

        public AcsCodeCheck()
        {
            Log4NetHelper.LogInit();
            NodeInfoHelper.SetConfig("nodes-env1-main");
            var firstNode = NodeInfoHelper.Config.Nodes.First();

            NodeManager = new NodeManager(firstNode.Endpoint);
            ContractManager = new ContractManager(NodeManager, firstNode.Account);
        }

        public INodeManager NodeManager { get; set; }
        public ContractManager ContractManager { get; set; }

        [TestMethod]
        public async Task QueryRequiredAcsContracts()
        {
            var acsResult = await ContractManager.ConfigurationStub.GetConfiguration.CallAsync(new StringValue
                {Value = nameof(ConfigurationNameProvider.RequiredAcsInContracts)});
            var acsInfo = RequiredAcsInContracts.Parser.ParseFrom(acsResult.Value);
            Logger.Info($"{JsonConvert.SerializeObject(acsInfo)}");
        }

        [TestMethod]
        public async Task SetEnableRequireAllAcs()
        {
            var transactionResult = ContractManager.Authority.ExecuteTransactionWithAuthority(
                ContractManager.Configuration.ContractAddress,
                nameof(ContractManager.ConfigurationStub.SetConfiguration),
                new SetConfigurationInput
                {
                    Key = nameof(ConfigurationNameProvider.RequiredAcsInContracts),
                    Value = new RequiredAcsInContracts
                    {
                        AcsList = {"acs1", "acs8"},
                        RequireAll = false
                    }.ToByteString()
                },
                ContractManager.CallAddress);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            await QueryRequiredAcsContracts();
        }
    }
}