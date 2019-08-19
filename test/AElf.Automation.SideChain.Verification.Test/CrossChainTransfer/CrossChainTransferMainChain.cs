using System.Collections.Generic;
using System.Threading;
using AElf.Automation.Common.Contracts;
using AElf.Types;

namespace AElf.Automation.SideChain.Verification.CrossChainTransfer
{
    public class CrossChainTransferMainChain : CrossChainTransferPrepare
    {
        public void CrossChainTransferMainChainJob()
        {
            CrossChainTransferOnMainChain(NativeToken);
            foreach (var symbol in TokenSymbol)
            {
                CrossChainTransferOnMainChain(symbol);
            }
        }

        private void CrossChainTransferOnMainChain(string symbol)
        {
            Logger.Info($"Main chain transfer {symbol} each side chain account");

            var mainRawTxInfos = new Dictionary<int, List<CrossChainTransactionInfo>>();

            foreach (var sideChainService in SideChainServices)
            {
                var rawTxInfos = new List<CrossChainTransactionInfo>();
                var resultTxInfos = new List<CrossChainTransactionInfo>();

                foreach (var mainAccount in AccountList[MainChainService.ChainId])
                {
                    foreach (var sideAccount in AccountList[sideChainService.ChainId])
                    {
                        var rawTxInfo = CrossChainTransferWithTxId(MainChainService, symbol, mainAccount,
                            sideAccount, sideChainService.ChainId, 100);
                        if (rawTxInfo == null) continue;

                        Thread.Sleep(100);
                        rawTxInfos.Add(rawTxInfo);
                    }
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
            Thread.Sleep(150000);


            foreach (var sideChainService in SideChainServices)
            {
                var sideChainReceiveTxIds = new List<CrossChainTransactionInfo>();
                Logger.Info($"Side chain {sideChainService.ChainId} receive the token {symbol}");
                foreach (var mainRawTxInfo in mainRawTxInfos[sideChainService.ChainId])
                {
                    Logger.Info("Check the index:");
                    while (!CheckSideChainBlockIndex(sideChainService, mainRawTxInfo))
                    {
                        Logger.Info("Block is not recorded ");
                        Thread.Sleep(10000);
                    }

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
                foreach (var result in resultList[TransactionResultStatus.Failed])
                {
                    Logger.Info(result.TxId);
                }

                Logger.Info("Receive Mined transaction:");
                foreach (var result in resultList[TransactionResultStatus.Mined])
                {
                    Logger.Info(result.TxId);
                }
            }

            Logger.Info("Show the main chain account balance: ");

            foreach (var mainAccount in AccountList[MainChainService.ChainId])
            {
                var accountBalance = GetBalance(MainChainService, mainAccount, symbol);
                Logger.Info($"Account:{mainAccount}, {symbol} balance is:{accountBalance}");
            }

            Logger.Info("Show the side chain account balance: ");
            foreach (var sideChainService in SideChainServices)
            {
                foreach (var sideAccount in AccountList[sideChainService.ChainId])
                {
                    var accountBalance = GetBalance(sideChainService, sideAccount, symbol);
                    Logger.Info($"Account:{sideAccount}, {symbol} balance is: {accountBalance}");
                }
            }
        }
    }
}