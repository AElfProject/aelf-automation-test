using System.Collections.Generic;
using System.Threading;
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

            var verifyBlock = mainChainBlockHeight > 1000 ? mainChainBlockHeight - 1000 : 1;
            Logger.Info($"Verify transaction with {verifyBlock}");

            while (true)
            {
                var mainChainTransactions = new Dictionary<long, List<string>>();
                var verifyInputs = new Dictionary<long, List<VerifyTransactionInput>>();
                var currentBlock = GetBlockHeight(MainChainService);
                if (verifyBlock >= currentBlock)
                {
                    verifyBlock = currentBlock - 1000;
                    Logger.Info($"Reset the verify block height:{verifyBlock}, waiting for index");
                    Thread.Sleep(60000);
                }

                //Get main chain transactions
                for (var i = verifyBlock; i < verifyBlock + VerifyBlockNumber; i++)
                {
                    var i1 = i;
                    var blockResult = AsyncHelper.RunSync(() =>
                        MainChainService.NodeManager.ApiService.GetBlockByHeightAsync(i1, true));
                    var txIds = blockResult.Body.Transactions;
                    mainChainTransactions.Add(i, txIds);

                    foreach (var txId in txIds)
                    {
                        Logger.Info(
                            $"Block {i} has transaction {txId}");
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
                    var verifyTxIds = new List<string>();
                    foreach (var verifyInput in verifyInputsValues)
                    {
                        foreach (var input in verifyInput)
                        {
                            var verifyTxId =
                                sideChainService.CrossChainService.ExecuteMethodWithTxId(
                                    CrossChainContractMethod.VerifyTransaction, input);
                            verifyTxIds.Add(verifyTxId);
                        }
                    }

                    CheckoutVerifyResult(sideChainService, verifyTxIds);
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