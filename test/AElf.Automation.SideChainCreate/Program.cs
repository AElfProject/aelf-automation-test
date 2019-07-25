using System;
using System.Collections.Generic;
using System.IO;
using AElf.Automation.Common.Helpers;

namespace AElf.Automation.SideChainCreate
{
    class Program
    {
        #region Private Properties

        private static readonly ILogHelper Logger = LogHelper.GetLogHelper();

        #endregion
        
        static void Main(string[] args)
        {
            #region Basic Preparation

            //Init Logger
            var logName = "CreateSideChain_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            Logger.InitLogHelper(dir);
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