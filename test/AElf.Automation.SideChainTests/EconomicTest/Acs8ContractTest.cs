using System;
using System.Threading.Tasks;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Kernel.SmartContract.ExecutionPluginForAcs8.Tests.TestContract;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Newtonsoft.Json;

namespace AElf.Automation.SideChainTests.EconomicTest
{
    public class Acs8ContractTest
    {
        public string Contract { get; set; }

        public ExecutionPluginForAcs8Contract PluginAcs8Contract { get; set; }
        public ContractServices Chain { get; set; }

        public ILog Logger = Log4NetHelper.GetLogger();

        public Acs8ContractTest(ContractServices chain, string contract)
        {
            Chain = chain;
            Contract = contract;
            PluginAcs8Contract = new ExecutionPluginForAcs8Contract(Chain.ApiHelper, Chain.CallAddress, Contract);
        }

        public async Task ExecutionTest()
        {
            var tester =  PluginAcs8Contract.GetTestStub<ContractContainer.ContractStub>(Chain.CallAddress);

            try
            {
                var cpuBalance = Chain.TokenService.GetUserBalance(Contract, "CPU");
                Logger.Info($"Contract 'CPU' balance: {cpuBalance}");
           
                //cpu
                var cpuResult = await tester.CpuConsumingMethod.SendAsync(new Empty());
                Logger.Info(JsonConvert.SerializeObject(cpuResult.TransactionResult));
           
                //net
                var randomBytes = CommonHelper.GenerateRandombytes(10240);
                var netResult = await tester.NetConsumingMethod.SendAsync(new NetConsumingMethodInput
                {
                    Blob = ByteString.CopyFrom(randomBytes)
                });
                Logger.Info(JsonConvert.SerializeObject(netResult.TransactionResult));

                //sto
                var stoResult = await tester.StoConsumingMethod.SendAsync(new Empty());
                Logger.Info(JsonConvert.SerializeObject(stoResult.TransactionResult));

                //few
                var fewResult = await tester.FewConsumingMethod.SendAsync(new Empty());
                Logger.Info(JsonConvert.SerializeObject(fewResult.TransactionResult));

            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
            }
        }
    }
}