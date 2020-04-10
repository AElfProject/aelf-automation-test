using Acs7;
using AElfChain.Common;
using AElfChain.Common.Helpers;
using log4net;

namespace AElf.Automation.SideChainCreate
{
    internal class Program
    {
        #region Private Properties

        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        #endregion

        private static void Main(string[] args)
        {
            #region Basic Preparation

            //Init Logger
            Log4NetHelper.LogInit("SideChainCreate");
            var testEnvironment = ConfigHelper.Config.TestEnvironment;
            var environmentInfo =
                ConfigHelper.Config.EnvironmentInfos.Find(o => o.Environment.Contains(testEnvironment));

            var configFile = environmentInfo.ConfigFile;
            NodeInfoHelper.SetConfig(configFile);

            #endregion

            var operation = new Operation();
            var sideChainInfos = ConfigHelper.Config.SideChainInfos;
            var approveTokenAmount = ConfigHelper.Config.ApproveTokenAmount;

            operation.TransferToken(1_000_000_00000000);
            operation.ApproveToken(approveTokenAmount);
            foreach (var sideChainInfo in sideChainInfos)
            {
                var tokenInfo = new SideChainTokenInfo
                {
                    Symbol = sideChainInfo.TokenSymbol,
                    TokenName = $"Side chain token {sideChainInfo.TokenSymbol}",
                    Decimals = 8,
                    IsBurnable = true,
                    IsProfitable = true,
                    Issuer = AddressHelper.Base58StringToAddress(operation.Creator),
                    TotalSupply = 10_00000000_00000000
                };
                var proposal = operation.RequestChainCreation(sideChainInfo.IndexingPrice,
                    sideChainInfo.LockedTokenAmount,
                    sideChainInfo.IsPrivilegePreserved, tokenInfo);
                Logger.Info($"Proposal is {proposal}");
                operation.ApproveProposal(proposal);
                var chainIdResult = operation.ReleaseSideChainCreation(proposal, out var organization);
                var chainId = ChainHelper.ConvertChainIdToBase58(chainIdResult);
                Logger.Info($"Association organization is {organization}");
                Logger.Info($"Side Chain : {chainId} created successfully");
            }
        }
    }
}