using System.Threading.Tasks;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using log4net;

namespace AElf.Automation.CheckTxStatus
{
    public class NodeServices
    {
        protected static string InitAccount;
        protected static string Password;
        protected readonly int VerifyBlockNumber;
        protected static string Url;
        protected static readonly ILog Logger = Log4NetHelper.GetLogger();

        public NodeServices()
        {
            var config = ConfigInfoHelper.Config;
            VerifyBlockNumber = config.VerifyBlockNumber;
            InitAccount = config.Account;
            Password = config.Password;
            Url = config.Url;
        }
    }
}