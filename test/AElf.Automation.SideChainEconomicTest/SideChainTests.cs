using System.Threading.Tasks;
using AElf.Automation.Common.OptionManagers.Authority;

namespace AElf.Automation.SideChainEconomicTest
{
    public class SideChainTests : TestBase
    {
        public const string Acs8ContractName = "AElf.Kernel.SmartContract.ExecutionPluginForAcs8.Tests.TestContract";
        
        public void GetTokenInfo()
        {
            Logger.Info("Query side chain token info");
            SideA.GetTokenInfos();
            
            SideA.GetTokenBalances(SideA.CallAddress);
        }

        public async Task<string> DeployContract_And_Transfer_Resources()
        {
            var authority = new AuthorityManager(SideA.ApiHelper, SideA.CallAddress);
            var deployContract = authority.DeployContractWithAuthority(SideA.CallAddress, Acs8ContractName);
            var acs8Contract = deployContract.GetFormatted();
            Logger.Info($"Acs8 contract address: {acs8Contract}");

            var genesisSide = SideA.GenesisService.GetGensisStub();
            var contractSide = await genesisSide.GetContractInfo.CallAsync(deployContract);
            Logger.Info($"Side contract info: {contractSide}");

            //transfer resource to acs8
            SideA.GetTokenBalances(SideA.CallAddress);
            SideA.TransferResources(SideA.CallAddress, acs8Contract, 2000_0000_0000);
            SideA.GetTokenBalances(acs8Contract);

            return acs8Contract;
        }
    }
}