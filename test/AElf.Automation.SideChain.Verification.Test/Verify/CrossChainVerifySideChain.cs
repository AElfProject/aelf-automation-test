using System.Collections.Generic;
using Acs7;
using AElf.Client.Dto;
using AElfChain.Common.Contracts;
using Google.Protobuf.WellKnownTypes;
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
            VerifySideChainTransaction(SideChainServices[VerifySideChainNumber - 1]);
        }

        private void VerifySideChainTransaction(ContractServices services)
        {
            var sideChainBlockHeight = GetBlockHeight(services);
            Logger.Info($"Side chain {services.ChainId} block height is {sideChainBlockHeight}");

            var verifyBlock = sideChainBlockHeight > 3000 ? sideChainBlockHeight - 3000 : 1;
            Logger.Info($"Verify transaction with {verifyBlock}");


            while (true)
            {
                var sideChainTransactions = new Dictionary<long, List<string>>();
                var verifyInputs = new Dictionary<long, List<VerifyTransactionInput>>();

                var indexSideHeight = GetIndexSideHeight(services);
                Logger.Info(
                    $"Main chain {MainChainService.ChainId} index side chain {services.ChainId} height {indexSideHeight}");

                if (verifyBlock >= indexSideHeight)
                {
                    verifyBlock = indexSideHeight - 3000;
                    Logger.Info($"Reset the verify block height:{verifyBlock}");
                }

                //Get side chain transactions
                for (var i = verifyBlock; i < verifyBlock + VerifyBlockNumber; i++)
                {
                    var i1 = i;
                    var blockResult =
                        AsyncHelper.RunSync(() => services.NodeManager.ApiClient.GetBlockByHeightAsync(i1, true));
                    var txIds = blockResult.Body.Transactions;
                    var resultsAsync = new List<TransactionResultDto>();
                    foreach (var txId in txIds)
                    {
                        var result = AsyncHelper.RunSync(() =>
                            services.NodeManager.ApiClient.GetTransactionResultAsync(txId));
                        resultsAsync.Add(result);
                    }

                    sideChainTransactions.Add(i, txIds);

                    foreach (var result in resultsAsync)
                        Logger.Info(
                            $"Block {i} has transaction {result.TransactionId} status {result.Status}");
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
                    var verifyResult = new Dictionary<string, bool>();
                    var sideBlock = sideChainService.NodeManager.ApiClient.GetBlockHeightAsync().Result;
                    Logger.Info($"On block height {sideBlock} get verify result: ");
                    foreach (var verifyInput in verifyInputsValues)
                    foreach (var input in verifyInput)
                    {
                        var result =
                            sideChainService.CrossChainService.CallViewMethod<BoolValue>(
                                CrossChainContractMethod.VerifyTransaction, input);
                        verifyResult.Add(input.TransactionId.ToHex(), result.Value);
                    }

                    GetVerifyResult(sideChainService, verifyResult);
                }

                Logger.Info($"Verify on the main chain {MainChainService.ChainId}");
                var mainVerifyResult = new Dictionary<string, bool>();
                var mainBlock = MainChainService.NodeManager.ApiClient.GetBlockHeightAsync().Result;
                Logger.Info($"On block height {mainBlock} get verify result: ");
                foreach (var verifyInput in verifyInputsValues)
                foreach (var input in verifyInput)
                {
                    var result = MainChainService.CrossChainService.CallViewMethod<BoolValue>(
                        CrossChainContractMethod.VerifyTransaction, input);
                    mainVerifyResult.Add(input.TransactionId.ToHex(), result.Value);
                }

                GetVerifyResult(MainChainService, mainVerifyResult);

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
            verificationInput.Path.MerklePathNodes.AddRange(
                crossChainMerkleProofContext.MerklePathFromParentChain.MerklePathNodes);

            verificationInput.ParentChainHeight = crossChainMerkleProofContext.BoundParentChainHeight;
            return verificationInput;
        }
    }
}