using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Standards.ACS7;
using AElf.Client.Dto;
using AElf.Types;
using AElfChain.Common.Contracts;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.Threading;

namespace AElf.Automation.SideChain.Verification.Verify
{
    public class CrossChainVerifyMainChain : CrossChainBase
    {
        private static long _verifyBlock;

        public CrossChainVerifyMainChain()
        {
            MainChainService = InitMainChainServices();
            SideChainServices = InitSideChainServices();
        }

        public void VerifyMainChainTransactionJob()
        {
            ExecuteStandaloneTask(new Action[]
            {
                VerifyMainChainTransaction
            });
        }

        private void VerifyMainChainTransaction()
        {
            var mainChainBlockHeight = GetBlockHeight(MainChainService);
            Logger.Info($"Main chain block height is {mainChainBlockHeight}");

            if (_verifyBlock == 0)
            {
                var sideChainHeight = new List<long>();
                foreach (var sideChainService in SideChainServices)
                {
                    var sideChainCreateHeight =
                        MainChainService.CrossChainService.GetChainInitializationData(sideChainService.ChainId)
                            .CreationHeightOnParentChain;
                    sideChainHeight.Add(sideChainCreateHeight);
                }
                _verifyBlock = sideChainHeight.Max();
            }

            var mainChainTransactions = new Dictionary<long, List<string>>();
            var verifyInputs = new Dictionary<long, List<VerifyTransactionInput>>();

            foreach (var sideChainService in SideChainServices)
            {
                var indexMainHeight = GetIndexParentHeight(sideChainService);
                Logger.Info($"Side chain {sideChainService.ChainId} index main chain height {indexMainHeight}");
                if (_verifyBlock > indexMainHeight) _verifyBlock = indexMainHeight- VerifyBlockNumber;
            }

            Logger.Info($"The verify block height:{_verifyBlock}");

            //Get main chain transactions
            for (var i = _verifyBlock; i < _verifyBlock + VerifyBlockNumber; i++)
            {
                var i1 = i;
                var blockResult = AsyncHelper.RunSync(() =>
                    MainChainService.NodeManager.ApiClient.GetBlockByHeightAsync(i1, true));
                var txIds = blockResult.Body.Transactions;
                var resultsAsync = new List<TransactionResultDto>();
                foreach (var txId in txIds)
                {
                    var result = MainChainService.NodeManager.ApiClient.GetTransactionResultAsync(txId).Result;
                    resultsAsync.Add(result);
                }

                mainChainTransactions.Add(i, txIds);

                foreach (var result in resultsAsync)
                    Logger.Info(
                        $"Block {i} has transaction {result.TransactionId} status {result.Status}");
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
                var verifyResult = new Dictionary<string, bool>();
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

            _verifyBlock += VerifyBlockNumber;
        }

        private VerifyTransactionInput GetMainChainTransactionVerificationInput(long blockHeight, string txId)
        {
            var merklePath = GetMerklePath(MainChainService, blockHeight, txId);
            if (merklePath == null) return null;

            var verificationInput = new VerifyTransactionInput
            {
                ParentChainHeight = blockHeight,
                TransactionId = Hash.LoadFromHex(txId),
                VerifiedChainId = MainChainService.ChainId,
                Path = merklePath
            };
            return verificationInput;
        }
    }
}