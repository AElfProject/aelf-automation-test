using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Automation.SideChain.Verification.CrossChainTransfer;
using AElf.Automation.SideChain.Verification.Verify;
using AElfChain.Common;
using AElfChain.Common.Helpers;
using log4net;

namespace AElf.Automation.SideChain.Verification
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            #region Basic Preparation

            //Init Logger
            Log4NetHelper.LogInit("CrossChainTest");
            TaskCollection = new List<Task>();

            var testEnvironment = ConfigInfoHelper.Config.TestEnvironment;
            var environmentInfo =
                ConfigInfoHelper.Config.EnvironmentInfos.Find(o => o.Environment.Contains(testEnvironment));
            var config = environmentInfo.Config;
            NodeInfoHelper.SetConfig(config);

            #endregion

            var register = new CrossChainRegister();
            var prepare = new CrossChainTransferPrepare();
            var create = new CrossChainCreateToken();
            var mainVerify = new CrossChainVerifyMainChain();
            var sideVerify = new CrossChainVerifySideChain();
            var mainTransfer = new CrossChainTransferMainChain();
            var sideTransfer = new CrossChainTransferSideChain();

            register.DoCrossChainPrepare();
            create.DoCrossChainCreateToken();
            prepare.DoCrossChainTransferPrepare();
            TaskCollection.Add(RunContinueJobWithInterval(mainVerify.VerifyMainChainTransactionJob, 600));
            TaskCollection.Add(RunContinueJobWithInterval(sideVerify.VerifySideChainTransactionJob,600));
            
            TaskCollection.Add(RunContinueJobWithInterval(mainTransfer.CrossChainTransferMainChainJob, 1200));
            TaskCollection.Add(RunContinueJobWithInterval(sideTransfer.CrossChainTransferSideChainJob, 1200));
            
            Task.WaitAll(TaskCollection.ToArray());
        }
        
        private static Task RunContinueJobWithInterval(Action action, int seconds)
        {
            void NewAction()
            {
                while (true)
                    try
                    {
                        action.Invoke();
                        Task.Delay(1000 * seconds);
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e);
                        break;
                    }
            }

            return Task.Run(NewAction);
        }

        #region Private Properties

        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private static List<Task> TaskCollection { get; set; }

        #endregion
    }
}