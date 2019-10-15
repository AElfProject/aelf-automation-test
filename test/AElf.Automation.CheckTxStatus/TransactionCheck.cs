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
        

        public TransactionCheck()
        {
            _nodeManager = new NodeManager(Url,AccountDir);
            _verifyBlock = VerifyBlockNumber;
        }

        public void CheckTxStatus()
        {
            long startBlock = 900;
            var blockHeight = _nodeManager.ApiService.GetBlockHeightAsync().Result;
            Logger.Info($"Chain block height is {blockHeight}");
            var verifyBlock = blockHeight > _verifyBlock ? _verifyBlock : blockHeight;
            
            while (true)
            {
                var currentBlock = _nodeManager.ApiService.GetBlockHeightAsync().Result;
                if (startBlock > currentBlock)
                    return;

                var transactionList = new Dictionary<long,Dictionary<string, TransactionInfo>>();
                var notExistTransactionList = new Dictionary<long,Dictionary<string, TransactionInfo>>();

                //Get transactions
                for (var i = startBlock; i < _verifyBlock+startBlock; i++)
                {
                    var i1 = i;
                    var blockResult = AsyncHelper.RunSync(() =>
                        _nodeManager.ApiService.GetBlockByHeightAsync(i1, true));
                    var txIds = blockResult.Body.Transactions;
                    var transactionInfos = new Dictionary<string, TransactionInfo>();
                    var notExistTransaction = new Dictionary<string,TransactionInfo>();
                    foreach (var txId in txIds)
                    {
                        var txResult = _nodeManager.ApiService.GetTransactionResultAsync(txId).Result;
                        var status = txResult.Status;
                        var transaction = txResult.Transaction;
                        var txInfo = new TransactionInfo(transaction,status);
                        if (status.Equals("NotExisted"))
                            notExistTransaction.Add(txId,txInfo);
                        transactionInfos.Add(txId,txInfo);
                    }
                
                    transactionList.Add(i, transactionInfos);
                    if (notExistTransaction.Count != 0)
                    {
                        notExistTransactionList.Add(i,notExistTransaction);
                        Logger.Info($"Block {i} has NotExisted transaction");
                        foreach (var transaction in notExistTransaction)
                        {
                            var info = $"Block {i}, Transaction {transaction.Key} status: {transaction.Value.Status}";
                            info +=
                                $"\r\n From:{transaction.Value.From},\n To:{transaction.Value.To},\n RefBlockNumber: {transaction.Value.RefBlockNumber},\n RefBlockPrefix: {transaction.Value.RefBlockPrefix},\n MethodName: {transaction.Value.MethodName}";
                            Logger.Error(info);
                        }
                    }
                    
                    foreach (var transaction in transactionInfos)
                    {
                        var info = $"Block {i}, Transaction {transaction.Key} status: {transaction.Value.Status}";
                        info +=
                            $"\r\n From:{transaction.Value.From},\n To:{transaction.Value.To},\n RefBlockNumber: {transaction.Value.RefBlockNumber},\n RefBlockPrefix: {transaction.Value.RefBlockPrefix},\n MethodName: {transaction.Value.MethodName}";

                        Logger.Info(info);
                    }
                }
                startBlock = _verifyBlock+startBlock;
            }
        }
    }
}