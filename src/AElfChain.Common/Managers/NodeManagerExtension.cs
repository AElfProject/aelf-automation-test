using System;
using System.Threading;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;

namespace AElfChain.Common.Managers
{
    public static class NodeManagerExtension
    {
        public static bool IsMainChain(this INodeManager nodeManager)
        {
            var genesis = nodeManager.GetGenesisContract();
            var token = genesis.GetTokenContract();
            var primaryToken = token.GetPrimaryTokenSymbol();
            var nativeToken = token.GetNativeTokenSymbol();

            return primaryToken == nativeToken;
        }

        public static string GetPrimaryTokenSymbol(this INodeManager nodeManager)
        {
            var genesis = nodeManager.GetGenesisContract();
            var token = genesis.GetTokenContract();
            var primaryToken = token.GetPrimaryTokenSymbol();

            return primaryToken;
        }

        public static string GetNativeTokenSymbol(this INodeManager nodeManager)
        {
            var genesis = nodeManager.GetGenesisContract();
            var token = genesis.GetTokenContract();
            var nativeToken = token.GetNativeTokenSymbol();

            return nativeToken;
        }

        public static TokenInfo GetTokenInfo(this INodeManager nodeManager, string symbol)
        {
            var genesis = nodeManager.GetGenesisContract();
            var token = genesis.GetTokenContract();

            return token.GetTokenInfo(symbol);
        }
        
        public static void WaitTransactionResultToLib(this INodeManager nodeManager, string transactionId)
        {
            var transactionResult = nodeManager.ApiService.GetTransactionResultAsync(transactionId).Result;
            if(transactionResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined)
                throw new Exception("Transaction result not Mined, no need wait lib to effective.");
            var transactionExecutionBlockNumber = transactionResult.BlockNumber;
            var checkTime = 60;
            while (true)
            {
                if(checkTime<=0)
                    throw new Exception("Transaction not increased to lib long time.");
                checkTime--;
                var chainStatus = nodeManager.ApiService.GetChainStatusAsync().Result;
                if (chainStatus.LastIrreversibleBlockHeight > transactionExecutionBlockNumber + 8)
                    break;
                Thread.Sleep(4000);
                Console.Write($"\rBlock height: {chainStatus.BestChainHeight}, Lib height: {chainStatus.LastIrreversibleBlockHeight}");
            }
        }

        public static void WaitCurrentHeightToLib(this INodeManager nodeManager)
        {
            var currentHeight = nodeManager.ApiService.GetBlockHeightAsync().Result;
            Thread.Sleep(4000);
            var checkTime = 60;
            while (true)
            {
                if(checkTime<=0)
                    throw new Exception("Transaction not increased to lib long time.");
                checkTime--;
                var chainStatus = nodeManager.ApiService.GetChainStatusAsync().Result;
                if (chainStatus.LastIrreversibleBlockHeight > currentHeight)
                {
                    Console.WriteLine();
                    break;
                }
                Thread.Sleep(4000);
                Console.Write($"\rBlock height: {chainStatus.BestChainHeight}, Lib height: {chainStatus.LastIrreversibleBlockHeight}, Wait target height: {currentHeight}");
            }
        }
    }
}