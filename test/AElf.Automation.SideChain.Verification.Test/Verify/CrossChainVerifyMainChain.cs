using System.Collections.Generic;
using System.Threading;
using AElf.Contracts.CrossChain;
using AElf.Sdk.CSharp;
using AElfChain.Common.Contracts;
using AElfChain.SDK.Models;
using Google.Protobuf.WellKnownTypes;
using AElfChain.Common.Contracts;
using AElf.Contracts.CrossChain;
using Volo.Abp.Threading;

namespace AElf.Automation.SideChain.Verification.Verify
{
    public class CrossChainVerifyMainChain : CrossChainBase
    {
        public CrossChainVerifyMainChain()
        {
            MainChainService = InitMainChainServices();
            SideChainServices = InitSideChainServices();
        }

        public void VerifyMainChainTransaction()
        {
            var mainChainBlockHeight = GetBlockHeight(MainChainService);
            Logger.Info($"Main chain block height is {mainChainBlockHeight}");

            var verifyBlock = mainChainBlockHeight > 3000 ? mainChainBlockHeight - 3000 : 1;
            Logger.Info($"Verify transaction with {verifyBlock}");

            while (true)
            {
                var mainChainTransactions = new Dictionary<long, List<string>>();
                var verifyInputs = new Dictionary<long, List<VerifyTransactionInput>>();
                
                foreach (var sideChainService in SideChainServices)
                {
                    var indexMainHeight = GetIndexParentHeight(sideChainService);
                    Logger.Info($"Side chain {sideChainService.ChainId} index main chain height {indexMainHeight}");
                    if (verifyBlock > indexMainHeight) verifyBlock = indexMainHeight - 3000;
                }
                
                Logger.Info($"Reset the verify block height:{verifyBlock}");
                
                //Get main chain transactions
                for (var i = verifyBlock; i < verifyBlock + VerifyBlockNumber; i++)
                {
                    var i1 = i;
                    var blockResult = AsyncHelper.RunSync(() =>
                        MainChainService.NodeManager.ApiService.GetBlockByHeightAsync(i1, true));
                    var txIds = blockResult.Body.Transactions;
                    var resultsAsync = new List<TransactionResultDto>();
                    foreach (var txId in txIds)
                    {
                        var result = MainChainService.NodeManager.ApiService.GetTransactionResultAsync(txId).Result;
                        resultsAsync.Add(result);
                    }
                    
                    mainChainTransactions.Add(i, txIds);

                    foreach (var result in resultsAsync)
                    {
                        Logger.Info(
                            $"Block {i} has transaction {result.TransactionId} status {result.Status}");
                    }
                }

                foreach (var mainChainTransaction in mainChainTransactions)
                {
                    var verifyInputList = new List<VerifyTransactionInput>();
                    var mainTxIds = mainChainTransaction.Value;
                    foreach (var txId in mainTxIds)
                    {
                        var verifyInput = GetMainChainTransactionVerificationInput(mainChainTransaction.Key, txId);
                        if (verifyInput == null) continue;
                        verifyInputList.Add(verifyInput);
                    }

                    verifyInputs.Add(mainChainTransaction.Key, verifyInputList);
                }
                
                foreach (var sideChainService in SideChainServices)
                {
                    Logger.Info($"Verify on the side chain {sideChainService.ChainId}");
                    var verifyInputsValues = verifyInputs.Values;
                    var verifyResult = new Dictionary<string,bool>();
                    foreach (var verifyInput in verifyInputsValues)
                    {
                        foreach (var input in verifyInput)
                        {
                            var result =
                                sideChainService.CrossChainService.CallViewMethod<BoolValue>(
                                    CrossChainContractMethod.VerifyTransaction, input);
                            verifyResult.Add(input.TransactionId.ToHex(),result.Value);
                        }
                    }

                    GetVerifyResult(sideChainService, verifyResult);
                }

                verifyBlock += VerifyBlockNumber;
            }
        }

        private VerifyTransactionInput GetMainChainTransactionVerificationInput(long blockHeight, string txId)
        {
            var merklePath = GetMerklePath(MainChainService, blockHeight, txId);
            if (merklePath == null) return null;

            var verificationInput = new VerifyTransactionInput
            {
                ParentChainHeight = blockHeight,
                TransactionId = HashHelper.HexStringToHash(txId),
                VerifiedChainId = MainChainService.ChainId,
                Path = merklePath
            };
            return verificationInput;
        }
    }
}