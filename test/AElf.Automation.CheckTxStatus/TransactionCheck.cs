using System;
using System.Collections.Generic;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using AElf.Kernel;
using AElf.Types;
using Volo.Abp.Threading;

namespace AElf.Automation.CheckTxStatus
{
    public class TransactionCheck : NodeServices
    {
        private string AccountDir { get; } = CommonHelper.GetCurrentDataDir();
        private readonly INodeManager _nodeManager;
        private readonly long _verifyBlock;
        private readonly long _startBlock;

        public TransactionCheck()
        {
            _nodeManager = new NodeManager(Url, AccountDir);
            _verifyBlock = VerifyBlockNumber;
            _startBlock = StartBlock;
        }

        public void CheckTxStatus()
        {
            var startBlock = _startBlock;
            var blockHeight = _nodeManager.ApiService.GetBlockHeightAsync().Result;
            Logger.Info($"Chain block height is {blockHeight}");
            var verifyBlock = blockHeight > _verifyBlock ? _verifyBlock : blockHeight;
            var notExistTransactionList = new Dictionary<long, Dictionary<string, TransactionInfo>>();
            var transactionInfos = new Dictionary<string, TransactionInfo>();

            while (true)
            {
                var currentBlock = _nodeManager.ApiService.GetBlockHeightAsync().Result;
                if (startBlock > currentBlock)
                    return;
                var transactionList = new Dictionary<long, List<string>>();

                var amount = verifyBlock + startBlock > currentBlock ? currentBlock : verifyBlock + startBlock;

                //Get transactions
                for (var i = startBlock; i < amount; i++)
                {
                    var i1 = i;
                    var blockResult = AsyncHelper.RunSync(() =>
                        _nodeManager.ApiService.GetBlockByHeightAsync(i1, true));
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
                        var txResult = _nodeManager.ApiService.GetTransactionResultAsync(txId).Result;
                        var status = txResult.Status;
                        var transaction = txResult.Transaction;
                        var txInfo = new TransactionInfo(transaction, status);
                        if (status.Equals("NotExisted"))
                            notExistTransaction.Add(txId, txInfo);
                        else
                        {
                            transactionPreBlock.Add(txId, txInfo);
                        }

                        try
                        {
                            transactionInfos.Add(txId, txInfo);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            var info =
                                $"Block {txs.Key}, Transaction {txId} status: {txInfo.Status}";
                            info +=
                                $"\r\n From:{txInfo.From},\n To:{txInfo.To},\n RefBlockNumber: {txInfo.RefBlockNumber},\n RefBlockPrefix: {txInfo.RefBlockPrefix},\n MethodName: {txInfo.MethodName}";
                            Logger.Error(info);
                            return;
                        }
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