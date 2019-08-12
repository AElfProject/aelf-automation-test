using System;
using System.IO;
using System.Linq;
using AElf.Automation.Common.Helpers;
using AElf.Automation.SideChain.Verification.CrossChainTransfer;
using AElf.Automation.SideChain.Verification.Verify;
using AElf.Types;
using FluentScheduler;

namespace AElf.Automation.SideChain.Verification
{
    class Program
    {
        #region Private Properties

        private static readonly ILogHelper Logger = LogHelper.GetLogHelper();

        #endregion

        public static void Main(string[] args)
        {
            #region Basic Preparation

            //Init Logger
            var logName = "CrossChain_" + DateTime.Now.ToString("MMddHHmmss") + ".log";
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", logName);
            Logger.InitLogHelper(dir);

            #endregion

            var enableCases = ConfigInfoHelper.Config.TestCases.FindAll(o => o.Enable).Select(o => o.Name).ToList();
            var createTokenNumber = ConfigInfoHelper.Config.CreateTokenNumber;
            var register = new CrossChainRegister();
            var prepare = new CrossChainTransferPrepare();
            var create = new CrossChainCreateToken();
            
            if (enableCases.Contains("CrossChainVerifyMainChain"))
            {
                var mainVerify = new CrossChainVerifyMainChain();
                mainVerify.VerifyMainChainTransaction();
            }

            if (enableCases.Contains("CrossChainVerifySideChain"))
            {
                var sideVerify = new CrossChainVerifySideChain();
                sideVerify.VerifySideChain();
            }

            if (!enableCases.Contains("CrossChainTransfer")) return;
            register.DoCrossChainPrepare();
            if (createTokenNumber >= 1)
            {
                create.DoCrossChainCreateToken();
            }
            prepare.DoCrossChainTransferPrepare();

            var mainTransfer = new CrossChainTransferMainChain();
            var sideTransfer = new CrossChainTransferSideChain();
            while (true)
            {
                mainTransfer.CrossChainTransferMainChainJob();
                sideTransfer.CrossChainTransferSideChainJob();
            }

        }
    }
}