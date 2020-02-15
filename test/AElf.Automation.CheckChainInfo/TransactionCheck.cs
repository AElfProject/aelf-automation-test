using System.Collections.Generic;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Volo.Abp.Threading;

namespace AElf.Automation.CheckTxStatus
{
    public class TransactionCheck : NodeServices
    {
        private readonly INodeManager _nodeManager;
        private readonly long _startBlock;
        private readonly long _verifyBlock;

        public TransactionCheck()
        {
            _nodeManager = new NodeManager(Url, AccountDir);
            _verifyBlock = VerifyBlockNumber;
            _startBlock = StartBlock;
        }

        private string AccountDir { get; } = CommonHelper.GetCurrentDataDir();

        public void CheckTxStatus()
        {
            var startBlock = _startBlock;
            var blockHeight = _nodeManager.ApiClient.GetBlockHeightAsync().Result;
            Logger.Info($"Chain block height is {blockHeight}");
            var verifyBlock = blockHeight > _verifyBlock ? _verifyBlock : blockHeight;
            var notExistTransactionList = new Dictionary<long, Dictionary<string, TransactionInfo>>();
            var transactionInfos = new HashSet<string>();

            while (true)
            {
                var currentBlock = _nodeManager.ApiClient.GetBlockHeightAsync().Result;
                if (startBlock > currentBlock)
                    return;
                var transactionList = new Dictionary<long, List<string>>();

                var amount = verifyBlock + startBlock > currentBlock ? currentBlock : verifyBlock + startBlock;

                //Get transactions
                for (var i = startBlock; i < amount; i++)
                {
                    var i1 = i;
                    var blockResult = AsyncHelper.RunSync(() =>
                        _nodeManager.ApiClient.GetBlockByHeightAsync(i1, true));
                    var txIds = blockResult.Body.Transactions;

                    transactionList.Add(i, txIds);
                }

                foreach (var txs in transactionList)
                {
                    var notExistTransaction = new Dictionary<string, TransactionInfo>();
                    var transactionPreBlock = new Dictionary<string, TransactionInfo>();
                    Logger.Info($"Transaction account in {txs.Key} block is {txs.Value.Count}");

                    foreach (var txId in txs.Value)
                    {
                        var txResult = _nodeManager.ApiClient.GetTransactionResultAsync(txId).Result;
                        var status = txResult.Status;
                        var transaction = txResult.Transaction;
                        var txInfo = new TransactionInfo(transaction, status);
                        if (status.Equals("NotExisted"))
                            notExistTransaction.Add(txId, txInfo);
                        else
                            transactionPreBlock.Add(txId, txInfo);

                        if (transactionInfos.Add(txId)) continue;
                        var info =
                            $"Block {txs.Key}, Transaction {txId} status: {txInfo.Status}";
                        info +=
                            $"\r\n From:{txInfo.From},\n To:{txInfo.To},\n RefBlockNumber: {txInfo.RefBlockNumber},\n RefBlockPrefix: {txInfo.RefBlockPrefix},\n MethodName: {txInfo.MethodName}";
                        Logger.Error(info);
                    }


                    if (notExistTransaction.Count != 0)
                    {
                        notExistTransactionList.Add(txs.Key, notExistTransaction);
                        Logger.Info($"Block {txs.Key} has NotExisted transaction");
                        foreach (var transaction in notExistTransaction)
                        {
                            var info =
                                $"Block {txs.Key}, Transaction {transaction.Key} status: {transaction.Value.Status}";
                            info +=
                                $"\r\n From:{transaction.Value.From},\n To:{transaction.Value.To},\n RefBlockNumber: {transaction.Value.RefBlockNumber},\n RefBlockPrefix: {transaction.Value.RefBlockPrefix},\n MethodName: {transaction.Value.MethodName}";
                            Logger.Error(info);
                        }
                    }

                    foreach (var transaction in transactionPreBlock)
                    {
                        var info = $"Block {txs.Key}, Transaction {transaction.Key} status: {transaction.Value.Status}";
                        Logger.Info(info);
                    }
                }

                startBlock = verifyBlock + startBlock;
            }
        }
    }
}