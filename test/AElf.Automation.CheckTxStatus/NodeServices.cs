using AElfChain.Common.Helpers;
using log4net;

namespace AElf.Automation.CheckTxStatus
{
    public class NodeServices
    {
        protected static string Url;
        protected static readonly ILog Logger = Log4NetHelper.GetLogger();
        protected readonly int StartBlock;
        protected readonly int VerifyBlockNumber;
        protected readonly string ExpectedContract;
        protected readonly string Account;

        public NodeServices()
        {
            var config = ConfigInfoHelper.Config;
            VerifyBlockNumber = config.VerifyBlockNumber;
            StartBlock = config.StartBlock;
            ExpectedContract = config.ExceptContract;
            Url = config.Url;
            Account = config.Account;
        }
    }
}