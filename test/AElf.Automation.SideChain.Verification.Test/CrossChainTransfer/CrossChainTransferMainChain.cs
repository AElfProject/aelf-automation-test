using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Types;
using AElfChain.Common.Contracts;

namespace AElf.Automation.SideChain.Verification.CrossChainTransfer
{
    public class CrossChainTransferMainChain : CrossChainTransferPrepare
    {
        public void CrossChainTransferMainChainJob()
        {
            foreach (var token in PrimaryTokens) CrossChainTransferOnMainChain(token);
            foreach (var symbol in TokenSymbols) CrossChainTransferOnMainChain(symbol);
        }

        private void CrossChainTransferOnMainChain(string symbol)
        {
            Logger.Info($"Main chain transfer {symbol} each side chain account");
            var tokenInfo = MainChainService.TokenService.GetTokenInfo(symbol);

            var mainRawTxInfos = new Dictionary<int, List<CrossChainTransactionInfo>>();

            foreach (var sideChainService in SideChainServices)
            {
                var rawTxInfos = new List<CrossChainTransactionInfo>();
                var resultTxInfos = new List<CrossChainTransactionInfo>();

                foreach (var account in AccountList)
                {
                    var rawTxInfo = CrossChainTransferWithTxId(MainChainService, symbol, account,
                        account, sideChainService.ChainId, tokenInfo.IssueChainId, 100);
                    if (rawTxInfo == null) continue;

                    Thread.Sleep(1000);
                    rawTxInfos.Add(rawTxInfo);
                }

                Logger.Info($"Check cross chain transfer result on chain {MainChainService.ChainId}:");
                foreach (var rawTxInfo in rawTxInfos)
                {
                    var resultTxInfo = GetCrossChainTransferResult(MainChainService, rawTxInfo);
                    if (resultTxInfo == null) continue;
                    resultTxInfos.Add(resultTxInfo);
                    Logger.Info(
                        $"The transactions block is:{resultTxInfo.BlockHeight},transaction id is: {resultTxInfo.TxId}");
                }

                mainRawTxInfos.Add(sideChainService.ChainId, resultTxInfos);
            }

            Logger.Info("Waiting for the index");
            Thread.Sleep(60000);

            foreach (var sideChainService in SideChainServices)
            {
                Logger.Info("Check the index:");
                while (!CheckSideChainBlockIndex(sideChainService, mainRawTxInfos[sideChainService.ChainId].Last()))
                {
                    Console.WriteLine("Block is not recorded ");
                    Thread.Sleep(10000);
                }

                var sideChainReceiveTxIds = new List<CrossChainTransactionInfo>();
                Logger.Info($"Side chain {sideChainService.ChainId} receive the token {symbol}");
                foreach (var mainRawTxInfo in mainRawTxInfos[sideChainService.ChainId])
                {
                    Logger.Info($"Receive CrossTransfer Transaction id is : {mainRawTxInfo.TxId}");
                    var receiveTokenInput = ReceiveFromMainChainInput(mainRawTxInfo);
                    sideChainService.TokenService.SetAccount(mainRawTxInfo.FromAccount);
                    var txId = sideChainService.TokenService.ExecuteMethodWithTxId(TokenMethod.CrossChainReceiveToken,
                        receiveTokenInput);
                    var txInfo = new CrossChainTransactionInfo(txId, mainRawTxInfo.ReceiveAccount);
                    sideChainReceiveTxIds.Add(txInfo);
                }

                Logger.Info($"Check cross chain receive result on chain {sideChainService.ChainId}:");
                var resultList = CheckoutTransferResult(sideChainService, sideChainReceiveTxIds);
                Logger.Error("Receive Failed transaction:");
                foreach (var result in resultList[TransactionResultStatus.Failed]) Logger.Info(result.TxId);

                Logger.Info("Receive Mined transaction:");
                foreach (var result in resultList[TransactionResultStatus.Mined]) Logger.Info(result.TxId);
            }

            CheckAccountBalance(symbol);
        }
    }
}