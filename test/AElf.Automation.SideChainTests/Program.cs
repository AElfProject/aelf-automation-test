using System;
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
            mainManager.GetContractTokenInfo("Acv7j84Ghi19JesSBQ8d56XenwCrJ5VBPvrS4mthtbuBjYtXR");
            var mBalance = mainManager.Token.GetUserBalance(ChainConstInfo.ChainAccount);
            logger.Info($"Main chain user balance: {mBalance}");

            //buy resource
            main.TokenService.TransferBalance(main.CallAddress, ChainConstInfo.TestAccount, 1000);
            
            await mainManager.GetTokenInfos();
            await mainManager.BuyResources(main.CallAddress, 400);

            //deploy on main chain
            var authorityMain = new AuthorityManager(main.ApiHelper, main.CallAddress);
            var deployMain = authorityMain.DeployContractWithAuthority(main.CallAddress, contractName);
            var contractAddress = deployMain.GetFormatted();
            var genesisMain = main.GenesisService.GetGensisStub();
            var contractMain = await genesisMain.GetContractInfo.CallAsync(deployMain);
            logger.Info($"Main contract info: {contractMain}");
            
            //transfer resource
            mainManager.TransferResourceToken(main, contractAddress);
            
            //get contract resource
            logger.Info("Before acs8 contract resource");
            mainManager.GetContractTokenInfo(contractAddress);
            
            logger.Info("Before consensus contract resource");
            mainManager.GetContractTokenInfo(main.ConsensusService.ContractAddress);

            //execution test cases
            var contractTest = new Acs8ContractTest(main, deployMain.GetFormatted());
            await contractTest.ExecutionTest();
            await Task.Delay(2000); //延迟检测共识资源币
            
            logger.Info("After acs8 contract resource");
            mainManager.GetContractTokenInfo(contractAddress);
            
            logger.Info("After consensus contract resource");
            mainManager.GetContractTokenInfo(main.ConsensusService.ContractAddress);

            Console.ReadLine();
            
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
            var authority = new AuthorityManager(sideA.ApiHelper, sideA.CallAddress);
            var deployContract = authority.DeployContractWithAuthority(sideA.CallAddress, contractName);
            var acs8Contract = deployContract.GetFormatted();

            var genesisSide = sideA.GenesisService.GetGensisStub();
            var contractSide = await genesisSide.GetContractInfo.CallAsync(deployContract);
            logger.Info($"Side contract info: {contractSide}");

            var contractDescriptor =
                 await sideA.ApiHelper.ApiService.GetContractFileDescriptorSetAsync(acs8Contract);
            logger.Info($"Contract file descriptor: {contractDescriptor.ToHex()}");
            
            //transfer resource to acs8
            sideManager.TransferResourceToken(sideA, acs8Contract);
            sideManager.GetContractTokenInfo(sideA, acs8Contract);
            
            var contract = new Acs8ContractTest(sideA, deployContract.GetFormatted());
            await contract.ExecutionTest();

            sBalanceA = sideA.TokenService.GetUserBalance(ChainConstInfo.ChainAccount);
            logger.Info($"Side chain A user balance: {sBalanceA}");
        }
    }
}