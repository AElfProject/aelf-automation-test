using System.Linq;
using AElf.Automation.SideChain.Verification.CrossChainTransfer;
using AElf.Automation.SideChain.Verification.Verify;
using AElfChain.Common;
using AElfChain.Common.Helpers;
using log4net;

namespace AElf.Automation.SideChain.Verification
{
    internal class Program
    {
        #region Private Properties

        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        #endregion

        public static void Main(string[] args)
        {
            #region Basic Preparation

            //Init Logger
            Log4NetHelper.LogInit("CrossChainTest");
            var testEnvironment = ConfigInfoHelper.Config.TestEnvironment;
            var environmentInfo =
                ConfigInfoHelper.Config.EnvironmentInfos.Find(o => o.Environment.Contains(testEnvironment));
            var config = environmentInfo.Config;
            NodeInfoHelper.SetConfig(config);

            #endregion

            var enableCases = ConfigInfoHelper.Config.TestCases.FindAll(o => o.Enable).Select(o => o.Name).ToList();
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
            create.DoCrossChainCreateToken();
            prepare.DoCrossChainTransferPrepare();

            var mainTransfer = new CrossChainTransferMainChain();
            var sideTransfer = new CrossChainTransferSideChain();
            for (var i = 1; i > 0; i++)
            {
                Logger.Info($"Transfer round {i} :");
                mainTransfer.CrossChainTransferMainChainJob();
                sideTransfer.CrossChainTransferSideChainJob();
            }
        }
    }
}