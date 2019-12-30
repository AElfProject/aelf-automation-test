using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenConverter;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class RentFeatureTest
    {
        public ILogHelper Logger = LogHelper.GetLogger();
        public INodeManager MainNode { get; set; }
        public INodeManager SideNode { get; set; }

        public RentFeatureTest()
        {
            Log4NetHelper.LogInit();
            Logger.InitLogHelper();
            MainNode = new NodeManager("192.168.197.40:8000");

            SideNode = new NodeManager("192.168.197.40:8001");
            NodeInfoHelper.SetConfig("nodes-env2-side1");
            Genesis = SideNode.GetGenesisContract();
        }

        public GenesisContract Genesis { get; set; }

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
                        {"RAM", 1000},
                        {"DISK", 10}
                    }
                }, organization, bps, bps.First());
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
            var symbols = new[] {"CPU", "RAM", "DISK"};
            foreach (var symbol in symbols)
            {
                await tokenConverter.Buy.SendAsync(new BuyInput
                {
                    Symbol = symbol,
                    Amount = 10000_00000000,
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