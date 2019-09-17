using System.Collections.Generic;
using System.Threading;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Contracts.CrossChain;
using AElfChain.SDK.Models;
using Volo.Abp.Threading;

namespace AElf.Automation.SideChain.Verification.Verify
{
    public class CrossChainVerifySideChain : CrossChainBase
    {
        public CrossChainVerifySideChain()
        {
            MainChainService = InitMainChainServices();
            SideChainServices = InitSideChainServices();
        }

        public void VerifySideChain()
        {
            VerifySideChainTransaction(SideChainServices[VerifySideChainNumber-1]);
        }

        private void VerifySideChainTransaction(ContractServices services)
        {
            var sideChainBlockHeight = GetBlockHeight(services);
            Logger.Info($"Side chain {services.ChainId} block height is {sideChainBlockHeight}");

            var verifyBlock = sideChainBlockHeight > 1000 ? sideChainBlockHeight - 1000 : 1;
            Logger.Info($"Verify transaction with {verifyBlock}");


            while (true)
            {
                var sideChainTransactions = new Dictionary<long, List<string>>();
                var verifyInputs = new Dictionary<long, List<VerifyTransactionInput>>();

                var currentBlock = GetBlockHeight(services);
                if (verifyBlock >= currentBlock)
                {
                    verifyBlock = currentBlock - 1000;
                    Logger.Info($"Reset the verify block height:{verifyBlock}, waiting for index");
                    Thread.Sleep(60000);
                }

                //Get side chain transactions
                for (var i = verifyBlock; i < verifyBlock + VerifyBlockNumber; i++)
                {
                    var i1 = i;
                    var blockResult =
                        AsyncHelper.RunSync(() => services.NodeManager.ApiService.GetBlockByHeightAsync(i1, true));
                    var txIds = blockResult.Body.Transactions;
                    sideChainTransactions.Add(i, txIds);

                    foreach (var txId in txIds)
                    {
                        Logger.Info(
                            $"Block {i} has transaction {txId}");
                    }
                }

                foreach (var sideChainTransaction in sideChainTransactions)
                {
                    var sideTxIds = sideChainTransaction.Value;
                    var verifyInputList = new List<VerifyTransactionInput>();
                    foreach (var txId in sideTxIds)
                    {
                        var verifyInput =
                            GetSideChainTransactionVerificationInput(services, sideChainTransaction.Key, txId);
                        verifyInputList.Add(verifyInput);
                    }

                    verifyInputs.Add(sideChainTransaction.Key, verifyInputList);
                }

                var verifyInputsValues = verifyInputs.Values;
                foreach (var sideChainService in SideChainServices)
                {
                    if (sideChainService == services) continue;
                    Logger.Info($"Verify on the side chain {sideChainService.ChainId}");
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

                Logger.Info($"Verify on the main chain {MainChainService.ChainId}");
                var mainVerifyTxIds = new List<string>();
                foreach (var verifyInput in verifyInputsValues)
                {
                    foreach (var input in verifyInput)
                    {
                        var verifyTxId = MainChainService.CrossChainService.ExecuteMethodWithTxId(
                            CrossChainContractMethod.VerifyTransaction, input);
                        mainVerifyTxIds.Add(verifyTxId);
                    }
                }

                CheckoutVerifyResult(MainChainService, mainVerifyTxIds);

                verifyBlock += VerifyBlockNumber;
            }
        }

        private VerifyTransactionInput GetSideChainTransactionVerificationInput(ContractServices services,
            long blockHeight, string txId)
        {
            var merklePath = GetMerklePath(services, blockHeight, txId);
            if (merklePath == null) return null;

            var verificationInput = new VerifyTransactionInput
            {
                TransactionId = HashHelper.HexStringToHash(txId),
                VerifiedChainId = services.ChainId,
                Path = merklePath
            };
            var crossChainMerkleProofContext = GetCrossChainMerkleProofContext(services, blockHeight);
            verificationInput.Path.MerklePathNodes.AddRange(crossChainMerkleProofContext.MerklePathForParentChainRoot.MerklePathNodes);
            verificationInput.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;

            return verificationInput;
        }
    }
}