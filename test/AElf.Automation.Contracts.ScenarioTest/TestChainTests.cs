using System.Linq;
using System.Threading.Tasks;
using AElf.Automation.Common;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using AElf.Contracts.TokenConverter;
using AElf.Types;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class TestChainTests
    {
        public INodeManager MainNode { get; set; }
        public INodeManager SideNode1 { get; set; }
        public INodeManager SideNode2 { get; set; }
        
        public string BpAccount { get; set; }

        public string TestSymbol = "STA";
        public ILog Logger { get; set; }

        public TestChainTests()
        {
            Log4NetHelper.LogInit();
            Logger = Log4NetHelper.GetLogger();
            
            MainNode = new NodeManager("18.212.240.254:8000");
            SideNode1 = new NodeManager("3.84.143.239:8000");
            SideNode2 = new NodeManager("34.224.27.242:8000");
            
            BpAccount = NodeInfoHelper.Config.Nodes.First().Account;
        }

        [TestMethod]
        [DataRow("", "TELF", 100_00000000)]
        public async Task TransferToken_Main(string to, string symbol, long amount)
        {
            var gensis = GenesisContract.GetGenesisContract(MainNode);
            var token = gensis.GetTokenContract();
            
            var beforeBalance = token.GetUserBalance(to, symbol);
            Logger.Info($"Before balance: {beforeBalance}");
            
            token.TransferBalance(BpAccount, to, amount, symbol);
            
            var afterBalance = token.GetUserBalance(to, symbol);
            Logger.Info($"After balance: {afterBalance}");
        }

        [TestMethod]
        [DataRow("TELF")]
        public async Task GetTokenConnector(string symbol)
        {
            var gensis = GenesisContract.GetGenesisContract(MainNode);
            var tokenConverter = gensis.GetTokenConverterStub();

            var result = await tokenConverter.GetConnector.CallAsync(new TokenSymbol
            {
                Symbol = symbol
            });
            
            Logger.Info($"Connector: {JsonConvert.SerializeObject(result)}");
        }

        [TestMethod]
        public async Task CreateConnector()
        {
            const long supply = 100_000_00000000;
            
            var gensis = GenesisContract.GetGenesisContract(MainNode);
            var tokenConverter = gensis.GetTokenConverterContract();
            
            var authority = new AuthorityManager(MainNode, BpAccount);
            var orgAddress = authority.GetGenesisOwnerAddress();
            var miners = authority.GetCurrentMiners();
            var connector = new Connector
            {
                Symbol = TestSymbol,
                IsPurchaseEnabled = true,
                IsVirtualBalanceEnabled = true,
                Weight = "0.5",
                VirtualBalance = supply
            };
            var transactionResult = authority.ExecuteTransactionWithAuthority(tokenConverter.ContractAddress,
                "SetConnector", connector, orgAddress, miners, BpAccount);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            await GetTokenConnector(TestSymbol);
        }

        [TestMethod]
        [DataRow(100_00000)]
        public async Task BuyResource(long amount)
        {
            var gensis = GenesisContract.GetGenesisContract(MainNode, BpAccount);
            var tokenConverter = gensis.GetTokenConverterStub();
            var token = gensis.GetTokenContract();
            
            Logger.Info($"Before {NodeOption.NativeTokenSymbol}: {token.GetUserBalance(BpAccount)}");
            Logger.Info($"Before {TestSymbol}: {token.GetUserBalance(BpAccount, TestSymbol)}");
            
            var transactionResult = await tokenConverter.Buy.SendAsync(new BuyInput
            {
                Symbol = TestSymbol,
                Amount = amount,
                PayLimit = 0
            });
            CheckTransactionResult(transactionResult.TransactionResult);

            Logger.Info($"After {NodeOption.NativeTokenSymbol}: {token.GetUserBalance(BpAccount)}");
            Logger.Info($"After {TestSymbol}: {token.GetUserBalance(BpAccount, TestSymbol)}");
        }

        [TestMethod]
        [DataRow(100_0000)]
        public async Task SellResource(long amount)
        {
            var gensis = GenesisContract.GetGenesisContract(MainNode, BpAccount);
            var tokenConverter = gensis.GetTokenConverterStub();
            var token = gensis.GetTokenContract();

            Logger.Info($"Before {NodeOption.NativeTokenSymbol}: {token.GetUserBalance(BpAccount)}");
            Logger.Info($"Before {TestSymbol}: {token.GetUserBalance(BpAccount, TestSymbol)}");

            var transactionResult = await tokenConverter.Buy.SendAsync(new BuyInput
            {
                Symbol = TestSymbol,
                Amount = amount,
                PayLimit = 0
            });
            CheckTransactionResult(transactionResult.TransactionResult);
            
            Logger.Info($"After {NodeOption.NativeTokenSymbol}: {token.GetUserBalance(BpAccount)}");
            Logger.Info($"After {TestSymbol}: {token.GetUserBalance(BpAccount, TestSymbol)}");
        }

        [TestMethod]
        [DataRow("TELF", 100_0000)]
        public async Task Transfer_From_Main_To_Side(string symbol, long amount)
        {
        }

        [TestMethod]
        [DataRow("")]
        public async Task SideChain_Accept_MainTransfer(string rawTransaction)
        {
        }
        
        [TestMethod]
        [DataRow("STA", 100_0000)]
        public async Task Transfer_From_Side_To_Main(string symbol, long amount)
        {
        }
        
        [TestMethod]
        [DataRow("")]
        public async Task MainChain_Accept_SideTransfer(string rawTransaction)
        {
        }
        
        private void CheckTransactionResult(TransactionResult result)
        {
            if(!result.Status.Equals(TransactionResultStatus.Mined))
                Logger.Error(result.Error);
        }
    }
}