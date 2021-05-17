using System;
using System.Threading;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using Volo.Abp.Threading;

namespace AElfChain.Common.Managers
{
    public static class NodeManagerExtension
    {
        public static void WaitTransactionResultToLib(this INodeManager nodeManager, string transactionId)
        {
            var transactionResult =
                AsyncHelper.RunSync(() => nodeManager.ApiClient.GetTransactionResultAsync(transactionId));
            if (transactionResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined)
                throw new Exception("Transaction result not Mined, no need wait lib to effective.");
            var transactionExecutionBlockNumber = transactionResult.BlockNumber;
            var checkTime = 60;
            while (true)
            {
                if (checkTime <= 0)
                    throw new Exception("Transaction not increased to lib long time.");
                checkTime--;
                var chainStatus = nodeManager.ApiClient.GetChainStatusAsync().Result;
                if (chainStatus.LastIrreversibleBlockHeight > transactionExecutionBlockNumber + 8)
                    break;
                Thread.Sleep(4000);
                Console.Write(
                    $"\rBlock height: {chainStatus.BestChainHeight}, Lib height: {chainStatus.LastIrreversibleBlockHeight}");
            }
        }
        
        public static void WaitOneBlock(this INodeManager nodeManager,long blockHeight)
        {
            var client = nodeManager.ApiClient;
            while (true)
            {
                var height = AsyncHelper.RunSync(client.GetBlockHeightAsync);
                if (height > blockHeight + 1)
                    return;
                Thread.Sleep(1000);
            }
        }

        public static void WaitCurrentHeightToLib(this INodeManager nodeManager)
        {
            var currentHeight = nodeManager.ApiClient.GetBlockHeightAsync().Result;
            Thread.Sleep(4000);
            var checkTime = 60;
            while (true)
            {
                if (checkTime <= 0)
                    throw new Exception("Transaction not increased to lib long time.");
                checkTime--;
                var chainStatus = AsyncHelper.RunSync(nodeManager.ApiClient.GetChainStatusAsync);
                if (chainStatus.LastIrreversibleBlockHeight > currentHeight)
                {
                    Console.WriteLine();
                    break;
                }

                Thread.Sleep(4000);
                Console.Write(
                    $"\rBlock height: {chainStatus.BestChainHeight}, Lib height: {chainStatus.LastIrreversibleBlockHeight}, Wait target height: {currentHeight}");
            }
        }
    }
}