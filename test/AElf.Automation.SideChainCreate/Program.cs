using System.Collections.Generic;
using AElf.Automation.Common.Helpers;
using log4net;

namespace AElf.Automation.SideChainCreate
{
    class Program
    {
        #region Private Properties

        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        #endregion
        
        static void Main(string[] args)
        {
            #region Basic Preparation

            //Init Logger
            Log4NetHelper.LogInit("SideChainCreate");

            #endregion

            var proposals = new List<string>();
            var operation = new Operation();
            var sideChainInfos = ConfigHelper.Config.SideChainInfos;
            var approveTokenAmount = long.Parse(ConfigHelper.Config.ApproveTokenAmount);
            
            operation.ApproveToken(approveTokenAmount);
            foreach (var sideChainInfo in sideChainInfos)
            {
                var proposal = operation.CreateProposal(sideChainInfo.IndexingPrice, sideChainInfo.LockedTokenAmount,
                    sideChainInfo.IsPrivilegePreserved);
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