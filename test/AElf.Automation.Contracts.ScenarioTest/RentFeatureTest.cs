using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
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

        public RentFeatureTest()
        {
            Log4NetHelper.LogInit();
            Logger.InitLogHelper();

            NodeManager = new NodeManager("192.168.197.40:8001");
            Genesis = NodeManager.GetGenesisContract();
        }

        public INodeManager NodeManager { get; set; }
        public GenesisContract Genesis { get; set; }

        [TestMethod]
        public void UpdateRentalTest()
        {
            var authority = new AuthorityManager(NodeManager);
            var token = Genesis.GetTokenContract();
            var organization = authority.GetGenesisOwnerAddress();
            var bps = authority.GetCurrentMiners();
            authority.ExecuteTransactionWithAuthority(token.CallAddress, nameof(TokenMethod.UpdateRental),
                new UpdateRentalInput
                {
                    Rental = {
                    {"CPU",1000},
                    {"RAM",500},
                    {"DISK",10}
                    }
                }, organization,bps, bps.First());
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
    }
}