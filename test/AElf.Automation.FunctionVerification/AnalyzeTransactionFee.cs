using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Volo.Abp.Threading;

namespace AElf.Automation.ContractsTesting
{
    public class AnalyzeTransactionFee
    {
        private const string Endpoint = "18.163.40.216:8000";

        public List<BlockDto> Blocks;
        public long TotalBlocks;
        public long TotalTransactions;
        public List<TransactionResultDto> TransactionResultDtos;

        public AnalyzeTransactionFee()
        {
            NodeManager = new NodeManager(Endpoint);
            Blocks = new List<BlockDto>();
            TransactionResultDtos = new List<TransactionResultDto>();
            TotalBlocks = 0;
            TotalTransactions = 0;
        }

        private INodeManager NodeManager { get; }

        public void QueryBlocksInfo(long fromHeight, long endHeight)
        {
            TotalBlocks = endHeight - fromHeight;
            Parallel.For(fromHeight, endHeight, i =>
            {
                var height = i;
                try
                {
                    var blockInfo =
                        AsyncHelper.RunSync(() => NodeManager.ApiClient.GetBlockByHeightAsync(height, true));
                    if (blockInfo.Body.TransactionsCount != 3)
                    {
                        $"Height: {height}, TxCount: {blockInfo.Body.TransactionsCount}".WriteSuccessLine();
                        Blocks.Add(blockInfo);
                    }
                }
                catch (Exception e)
                {
                    e.Message.WriteErrorLine();
                }
            });

            TotalTransactions = Blocks.Sum(o => o.Body.TransactionsCount);
            TotalTransactions += 3 * (TotalBlocks - Blocks.Count);
            $"Total blocks: {TotalBlocks}, Total transactions: {TotalTransactions}, Average transaction: {TotalTransactions / TotalBlocks}"
                .WriteSuccessLine();
        }

        public void QueryTransactionsInfo()
        {
            foreach (var block in Blocks)
            {
                var transactions = block.Body.Transactions;
                Parallel.ForEach(transactions, txId =>
                {
                    try
                    {
                        var transactionResult =
                            AsyncHelper.RunSync(() => NodeManager.ApiClient.GetTransactionResultAsync(txId));
                        var transactionFee = transactionResult.GetDefaultTransactionFee();
                        $"TxId: {txId}, Fee: {transactionFee}".WriteSuccessLine();
                        TransactionResultDtos.Add(transactionResult);
                    }
                    catch (Exception e)
                    {
                        e.Message.WriteErrorLine();
                    }
                });
            }
        }

        public void CalculateTotalFee()
        {
            long totalFee = 0;
            var count = TransactionResultDtos.Count;
            foreach (var transactionFee in TransactionResultDtos) totalFee += transactionFee.GetDefaultTransactionFee();

            $"Total blocks: {TotalBlocks}, Total transactions: {TotalTransactions}".WriteSuccessLine();
            $"Count: {count}, TotalFees: {totalFee}, AverageFee: {totalFee / count}".WriteSuccessLine();
        }
    }
}