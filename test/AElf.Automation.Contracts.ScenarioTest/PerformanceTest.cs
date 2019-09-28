using System.Linq;
using System.Threading.Tasks;
using AElf.Automation.Common;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using AElf.Contracts.TestContract.Performance;
using Google.Protobuf;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class PerformanceTest
    {
        public ILogHelper Logger = LogHelper.GetLogger();
        public INodeManager NodeManager { get; set; }
        
        public string TestAccount { get; set; }
        
        public PerformanceContractContainer.PerformanceContractStub PerformanceStub { get; set; }
        
        public PerformanceTest()
        {
            Log4NetHelper.LogInit();
            Logger.InitLogHelper();
            
            NodeManager = new NodeManager("192.168.197.40:8000");
            TestAccount = NodeManager.GetRandomAccount();
        }

        [TestMethod]
        public void DeployContract_Test()
        {
            var token = NodeManager.GetGenesisContract().GetTokenContract();
            var bp = NodeOption.AllNodes.First().Account;
            token.TransferBalance(bp, TestAccount, 1000);
            
            var contract = new PerformanceContract(NodeManager, TestAccount);
            Logger.Info($"Performance contract address: {contract.ContractAddress}");
        }

        [TestMethod]
        [DataRow("B2HK7R8HPDdR7t8J7U2ChN6NVYYA81ZmphhH8szavGX6WuybT")]
        public async Task ExecutePerformance_ComputeTest(string contract)
        {
            var performance = new PerformanceContract(NodeManager, TestAccount, contract);
            PerformanceStub =
                performance.GetTestStub<PerformanceContractContainer.PerformanceContractStub>(TestAccount);
            for (var i = 1; i < 5; i++)
            {
                var transactionResult = await PerformanceStub.ComputeLevel.SendAsync(new NumberInput
                {
                    Number = i * 10
                });
                Logger.Info($"Test number: {i * 5}, TransactionId: {transactionResult.TransactionResult.TransactionId}, Status: {transactionResult.TransactionResult.Status}");
            }
        }
        
        [TestMethod]
        [DataRow("B2HK7R8HPDdR7t8J7U2ChN6NVYYA81ZmphhH8szavGX6WuybT")]
        public async Task ExecutePerformance_WriteTest(string contract)
        {
            var performance = new PerformanceContract(NodeManager, TestAccount, contract);
            PerformanceStub =
                performance.GetTestStub<PerformanceContractContainer.PerformanceContractStub>(TestAccount);
            for (var i = 1; i < 10; i++)
            {
                var transactionResult = await PerformanceStub.WriteKContentByte.SendAsync(new WriteInput
                {
                    Content = ByteString.CopyFrom(CommonHelper.GenerateRandombytes(1024 * i))
                });
                Logger.Info($"Test number: {i * 10}, TransactionId: {transactionResult.TransactionResult.TransactionId}, Status: {transactionResult.TransactionResult.Status}");
            }
        }

        [TestMethod]
        [DataRow("B2HK7R8HPDdR7t8J7U2ChN6NVYYA81ZmphhH8szavGX6WuybT")]
        public async Task QueryPerformance_Test(string contract)
        {
            var performance = new PerformanceContract(NodeManager, TestAccount, contract);
            PerformanceStub =
                performance.GetTestStub<PerformanceContractContainer.PerformanceContractStub>(TestAccount);
            for (var i = 1; i < 30; i++)
            {
                var transactionResult = await PerformanceStub.QueryFibonacci.SendAsync(new NumberInput
                {
                    Number = i
                });
                Logger.Info($"Test number: {i * 10}, TransactionId: {transactionResult.TransactionResult.TransactionId}, Status: {transactionResult.TransactionResult.Status}");
            }
        }
    }
}