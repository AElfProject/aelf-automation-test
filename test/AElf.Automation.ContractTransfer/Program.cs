using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using log4net;

namespace AElf.Automation.ContractTransfer
{
    class Program
    {
        static void Main(string[] args)
        {
            Log4NetHelper.LogInit("ContractTransaction");

            var transfer = new TransferAction();

            // Deploy contract for test 
            _wrapperContractList = transfer.DeployWrapperContractWithAuthority(out TokenContract tokenContract);

            // Create or Get token
            _wrapperInfoList = transfer.CreateAndIssueTokenForWrapper(_wrapperContractList,tokenContract);
            
            //Transfer Task
            var cts = new CancellationTokenSource();
            var token = cts.Token;
            var taskList = new List<Task>
            {
                Task.Run(() => transfer.ContinueContractTransfer(_wrapperInfoList, cts, token), token),
            };

            Task.WaitAll(taskList.ToArray<Task>());
        }

        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private static List<TransferWrapperContract> _wrapperContractList;
        private static Dictionary<TransferWrapperContract, string> _wrapperInfoList;
    }
}