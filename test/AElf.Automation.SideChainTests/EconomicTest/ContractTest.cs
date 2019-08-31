using System.Threading.Tasks;
using Acs8;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Contracts.TokenConverter;
using AElf.Kernel.SmartContract.ExecutionPluginForAcs8.Tests.TestContract;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Newtonsoft.Json;
using Shouldly;

namespace AElf.Automation.SideChainTests.EconomicTest
{
    public class ContractTest
    {
        public string _contract { get; set; }

        public ExecutionPluginForAcs8Contract _pluginAcs8Contract { get; set; }
        public ContractServices _chain { get; set; }

        public ILog Logger = Log4NetHelper.GetLogger();

        public ContractTest(ContractServices chain, string contract)
        {
            _chain = chain;
            _contract = contract;
            _pluginAcs8Contract = new ExecutionPluginForAcs8Contract(_chain.ApiHelper, _chain.CallAddress, _contract);
        }

        public async Task DeployContractAndSetResource()
        {
            
        }

        public async Task ExecutionTest()
        {
           var tester =  _pluginAcs8Contract.GetTestStub<ContractContainer.ContractStub>(_chain.CallAddress);
           
           //buy resource
           var buyResult = await tester.BuyResourceToken.SendAsync(new BuyResourceTokenInput
           {
               Symbol = "CPU",
               Amount = 2000_0000_0000,
           });
           buyResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

           var cpuBalance = _chain.TokenService.GetUserBalance(_contract, "CPU");
           Logger.Info($"Contract 'CPU' balance: {cpuBalance}");
           
           //cpu
           var cpuResult = await tester.CpuConsumingMethod.SendAsync(new Empty());
           Logger.Info(JsonConvert.SerializeObject(cpuResult));
           
           //net
           var netResult = await tester.NetConsumingMethod.SendAsync(new NetConsumingMethodInput
           {
                Blob = ByteString.CopyFromUtf8("test")
           });
           Logger.Info(JsonConvert.SerializeObject(netResult));

           //sto
           var stoResult = await tester.StoConsumingMethod.SendAsync(new Empty());
           Logger.Info(JsonConvert.SerializeObject(stoResult));

           //few
           var fewResult = await tester.FewConsumingMethod.SendAsync(new Empty());
           Logger.Info(JsonConvert.SerializeObject(fewResult));
        }
    }
}