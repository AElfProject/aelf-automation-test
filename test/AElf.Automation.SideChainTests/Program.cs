using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.OptionManagers.Authority;
using AElf.Automation.SideChainTests.EconomicTest;
using Volo.Abp.Threading;

namespace AElf.Automation.SideChainTests
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            const string contractName = "AElf.Kernel.SmartContract.ExecutionPluginForAcs8.Tests.TestContract";
            
            //Init Logger
            Log4NetHelper.LogInit("CrossChainTest");
            var logger = Log4NetHelper.GetLogger();

            //Test
            var mainManager = new MainChainManager(ChainConstInfo.MainChainUrl, ChainConstInfo.ChainAccount);
            var main = mainManager.MainChain;
            var mBalance = mainManager.Token.GetUserBalance(ChainConstInfo.ChainAccount);
            logger.Info($"Main chain user balance: {mBalance}");

            //buy resource
            main.TokenService.TransferBalance(main.CallAddress, ChainConstInfo.TestAccount, 1000);
            
            await mainManager.GetTokenInfos(new List<string> {"CPU", "NET", "STO"});
            await mainManager.BuyResource(main.CallAddress, "CPU", 200);
            await mainManager.BuyResource(main.CallAddress, "NET", 200);
            await mainManager.BuyResource(main.CallAddress, "STO", 200);

            //deploy on main chain
            var authorityMain = new AuthorityManager(main.ApiHelper.GetApiUrl(), main.CallAddress);
            var deployMain = authorityMain.DeployContractWithAuthority(main.CallAddress, contractName);
            var genesisMain = main.GenesisService.GetBasicContractTester();
            var contractMain = await genesisMain.GetContractInfo.CallAsync(deployMain);
            logger.Info($"Main contract info: {contractMain}");
            
            //transfer resource
            main.TokenService.TransferBalance(main.CallAddress, deployMain.GetFormatted(), 200_0000_0000, "CPU");
            main.TokenService.TransferBalance(main.CallAddress, deployMain.GetFormatted(), 200_0000_0000, "NET");
            main.TokenService.TransferBalance(main.CallAddress, deployMain.GetFormatted(), 200_0000_0000, "STO");

            var contractTest = new ContractTest(main, deployMain.GetFormatted());
            AsyncHelper.RunSync(contractTest.ExecutionTest);
            
            //deploy on side chain
            var sideManager = new SideChainManager();
            var sideA = sideManager.InitializeSideChain(ChainConstInfo.SideChainUrlA, ChainConstInfo.ChainAccount,
                ChainConstInfo.SideChainIdA);
            var sideB = sideManager.InitializeSideChain(ChainConstInfo.SideChainUrlB, ChainConstInfo.ChainAccount,
                ChainConstInfo.SideChainIdB);
            
            var sBalanceA = sideA.TokenService.GetUserBalance(ChainConstInfo.ChainAccount);
            logger.Info($"Side chain A user balance: {sBalanceA}");

            var sBalanceB = sideB.TokenService.GetUserBalance(ChainConstInfo.ChainAccount);
            logger.Info($"Side chain B user balance: {sBalanceB}");
            
            logger.Info("Test side chain.");
            var authority = new AuthorityManager(sideA.ApiHelper.GetApiUrl(), sideA.CallAddress);
            var deployContract = authority.DeployContractWithAuthority(sideA.CallAddress, contractName);
            logger.Info($"{deployContract}");

            Thread.Sleep(30);

            var genesisSide = sideA.GenesisService.GetBasicContractTester();
            var contractSide = await genesisSide.GetContractInfo.CallAsync(deployContract);
            logger.Info($"Side contract info: {contractSide}");

            var contractDescriptor =
                AsyncHelper.RunSync(() =>
                    sideA.ApiHelper.ApiService.GetContractFileDescriptorSetAsync(deployContract.GetFormatted()));
            logger.Info($"Contract file descriptor: {contractDescriptor.ToHex()}");

            var contract = new ContractTest(sideA, deployContract.GetFormatted());
            AsyncHelper.RunSync(contract.ExecutionTest);

            sBalanceA = sideA.TokenService.GetUserBalance(ChainConstInfo.ChainAccount);
            logger.Info($"Side chain A user balance: {sBalanceA}");
        }
    }
}