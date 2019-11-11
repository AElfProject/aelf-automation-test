using System.Threading.Tasks;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;

namespace AElf.Automation.CheckTxStatus
{
    public class NodeServices
    {
        protected readonly int VerifyBlockNumber;
        protected readonly int StartBlock;
        protected static string Url;
        protected static readonly ILog Logger = Log4NetHelper.GetLogger();

        public NodeServices()
        {
            var config = ConfigInfoHelper.Config;
            VerifyBlockNumber = config.VerifyBlockNumber;
            StartBlock = config.StartBlock;
            Url = config.Url;
        }
    }
}