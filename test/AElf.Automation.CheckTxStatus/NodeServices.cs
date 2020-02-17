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

        public NodeServices()
        {
            var config = ConfigInfoHelper.Config;
            VerifyBlockNumber = config.VerifyBlockNumber;
            StartBlock = config.StartBlock;
            Url = config.Url;
        }
    }
}