using System.Collections.Generic;
using Acs7;
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

            #endregion

            var proposals = new List<string>();
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
                    Issuer = AddressHelper.Base58StringToAddress(operation.InitAccount),
                    TotalSupply = 10_00000000_00000000
                };
                var proposal = operation.CreateProposal(sideChainInfo.IndexingPrice, sideChainInfo.LockedTokenAmount,
                    sideChainInfo.IsPrivilegePreserved, tokenInfo);
                proposals.Add(proposal);
            }

            foreach (var proposal in proposals)
            {
                operation.ApproveProposal(proposal);
                var chainIdResult = operation.ReleaseProposal(proposal);
                var chainId = ChainHelper.ConvertChainIdToBase58(chainIdResult);
                Logger.Info($"Side Chain : {chainId} created successfully");
            }
        }
    }
}