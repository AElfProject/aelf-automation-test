using System;
using System.Linq;
using System.Threading.Tasks;
using AElfChain.Common;
using AElfChain.Common.Helpers;

namespace AElf.Automation.SideChainEconomicTest
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {
            Log4NetHelper.LogInit();
            var logger = Log4NetHelper.GetLogger();
            var sideTest = new SideChainTests();
            sideTest.GetTokenInfo();

            await sideTest.SideManager.RunCheckSideChainTokenInfo(sideTest.SideA, "SAELF");

            Console.ReadLine();

            var mainTest = new MainChainTests();
            var acs8Contract = "";
            if (acs8Contract == "")
            {
                await mainTest.MainManager.BuyResources(ChainConstInfo.ChainAccount, 2000);
                await mainTest.Transfer_From_Main_To_Side();

                acs8Contract = await sideTest.DeployContract_And_Transfer_Resources();
            }

            /* comment due to Acs8ContractTest removed on dev
            var contract = new Acs8ContractTest(sideTest.SideA, acs8Contract);
            await contract.ExecutionTest();
            await Task.Delay(50);
            */

            sideTest.SideA.GetTokenBalances(acs8Contract);

            logger.Info("Get side chain consensus resource tokens");
            var consensus = sideTest.SideA.ConsensusService;
            sideTest.SideA.GetTokenBalances(consensus.ContractAddress);

            //Query all main bp resources
            logger.Info("Get side chain bps resource tokens");
            var bps = NodeInfoHelper.Config.Nodes.Select(o => o.Account);
            foreach (var bp in bps) sideTest.SideA.GetTokenBalances(bp);
        }
    }
}