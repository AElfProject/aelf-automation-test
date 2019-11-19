using System.Linq;
using AElfChain.Common.Helpers;
using AElf.Automation.SideChain.Verification.CrossChainTransfer;
using AElf.Automation.SideChain.Verification.Verify;
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
            if (createTokenNumber >= 1) create.DoCrossChainCreateToken();
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