using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenConverter;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class SidechainRentFeatureTests
    {
        public ILogHelper Logger = LogHelper.GetLogger();
        public INodeManager MainNode { get; set; }
        public INodeManager SideNode { get; set; }
        
        public GenesisContract Genesis { get; set; }

        public SidechainRentFeatureTests()
        {
            Log4NetHelper.LogInit();
            Logger.InitLogHelper();
            MainNode = new NodeManager("13.236.178.147:8000");

            NodeInfoHelper.SetConfig("nodes-online-test-side1");
            var bpNode = NodeInfoHelper.Config.Nodes.First();
            SideNode = new NodeManager(bpNode.Endpoint);
            Genesis = SideNode.GetGenesisContract(bpNode.Account);
        }

        [TestMethod]
        public void UpdateRentalTest()
        {
            var authority = new AuthorityManager(SideNode);
            var token = Genesis.GetTokenContract();
            var organization = authority.GetGenesisOwnerAddress();
            var bps = authority.GetCurrentMiners();
            authority.ExecuteTransactionWithAuthority(token.ContractAddress, nameof(TokenMethod.UpdateRental),
                new UpdateRentalInput
                {
                    Rental =
                    {
                        {"CPU", 1000},
                        {"RAM", 500},
                        {"DISK", 4},
                        {"NET", 2}
                    }
                }, organization, bps, bps.First());
        }

        [TestMethod]
        public async Task UpdateRentedResources()
        {
            var token = Genesis.GetTokenStub();
            var transactionResult = await token.UpdateRentedResources.SendAsync(new UpdateRentedResourcesInput
            {
                ResourceAmount =
                {
                    {"CPU", 2},
                    {"RAM", 4},
                    {"DISK", 512}
                }
            });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var resourceUsage = await token.GetResourceUsage.CallAsync(new Empty());
            resourceUsage.Value["CPU"].ShouldBe(2);
            resourceUsage.Value["RAM"].ShouldBe(4);
            resourceUsage.Value["DISK"].ShouldBe(512);
        }

        [TestMethod]
        public async Task QueryResourceUsage()
        {
            var token = Genesis.GetTokenStub();
            var resourceUsage = await token.GetResourceUsage.CallAsync(new Empty());
            Logger.Info(JsonConvert.SerializeObject(resourceUsage));
        }

        [TestMethod]
        public async Task QueryOwningRental()
        {
            var token = Genesis.GetTokenStub();
            var rental = await token.GetOwningRental.CallAsync(new Empty());
            foreach (var item in rental.ResourceAmount)
            {
                Logger.Info($"{item.Key}, {item.Value}");
            }
        }

        [TestMethod]
        public async Task MainChain_BuyResource()
        {
            var authority = new AuthorityManager(MainNode);
            var bps = authority.GetCurrentMiners();

            var genesis = MainNode.GetGenesisContract(bps.First());
            var token = genesis.GetTokenContract();
            var tokenConverter = genesis.GetTokenConverterStub();
            var symbols = new[] {"CPU", "RAM", "DISK", "NET"};
            foreach (var symbol in symbols)
            {
                await tokenConverter.Buy.SendAsync(new BuyInput
                {
                    Symbol = symbol,
                    Amount = 10000_00000000
                });
            }

            Logger.Info($"Account: {bps.First()}");
            foreach (var symbol in symbols)
            {
                var balance = token.GetUserBalance(bps.First(), symbol);
                Logger.Info($"{symbol}={balance}");
            }
        }
    }
}