using System.IO;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volo.Abp.Threading;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class RpcApiTest
    {
        private const string ServiceUrl = "http://192.168.197.15:8020";
        private readonly ILogHelper _logger = LogHelper.GetLogger();
        private INodeManager Ch { get; set; }

        [TestInitialize]
        public void InitTest()
        {
            //Init Logger
            var logName = "RpcApiTest.log";
            var dir = Path.Combine(CommonHelper.AppRoot, "logs", logName);
            _logger.InitLogHelper(dir);

            Ch = new NodeManager(ServiceUrl);
        }

        [TestMethod]
        [DataRow(2441)]
        public void VerifyTransactionByHeight(int height)
        {
            var blockDto = AsyncHelper.RunSync(() => Ch.ApiClient.GetBlockByHeightAsync(height, true));
            var txArray = blockDto.Body.Transactions;

            foreach (var txId in txArray)
            {
                var transactionResult = AsyncHelper.RunSync(() => Ch.ApiClient.GetTransactionResultAsync(txId));
                var status = transactionResult.Status;
                if (status.ConvertTransactionResultStatus() == TransactionResultStatus.Mined)
                    _logger.Info($"{txId}: mined");
                else
                    _logger.Error(transactionResult.Error);
            }
        }
    }
}