using System.Collections.Generic;
using System.Threading;
using AElf.Automation.Common.Contracts;
using AElf.Types;

namespace AElf.Automation.SideChain.Verification.CrossChainTransfer
{
    public class CrossChainTransferSideChain : CrossChainTransferPrepare
    {
        
        public void CrossChainTransferSideChainJob()
        {
            CrossChainTransferOnSideChain(NativeToken);

            if (TokenSymbol.Count < 1) return;
            foreach (var symbol in TokenSymbol)
            {
                CrossChainTransferOnSideChain(symbol);
            }
        }

        private void CrossChainTransferOnSideChain(string symbol)
        {
            foreach (var sideChainService in SideChainServices)
            {
                Logger.Info($"Side chain {sideChainService.ChainId} transfer {symbol} to each account");
                var sideRawTxInfos = new Dictionary<int, List<CrossChainTransactionInfo>>();
                var sideResultTxInfos =new  List<CrossChainTransactionInfo>();

                var mainRawTxInfos = new List<CrossChainTransactionInfo>();
                var mainResultTxInfos = new List<CrossChainTransactionInfo>();


                Logger.Info($"Side chain {sideChainService.ChainId} transfer to {MainChainService.ChainId}");

                foreach (var transferSideAccount in AccountList[sideChainService.ChainId])
                {
                    // to main chain account
                    foreach (var receiveSideAccount in AccountList[MainChainService.ChainId])
                    {
                        var rawTxInfo = CrossChainTransferWithTxId(sideChainService, symbol, transferSideAccount,
                            receiveSideAccount, MainChainService.ChainId, 100);
                        if (rawTxInfo == null) continue;

                        Thread.Sleep(100);
                        mainRawTxInfos.Add(rawTxInfo);
                    }
                }

                Logger.Info($"Check cross chain transfer result on chain {sideChainService.ChainId}:");
                foreach (var mainRawTx in mainRawTxInfos)
                {
                    var resultTxInfo = GetCrossChainTransferResult(sideChainService, mainRawTx);
                    if (resultTxInfo == null) continue;
                    mainResultTxInfos.Add(resultTxInfo);
                    Logger.Info(
                        $"The transactions block is:{resultTxInfo.BlockHeight},transaction id is: {resultTxInfo.TxId}");
                }
                

                foreach (var receiveSideChain in SideChainServices)
                {
                    if (receiveSideChain == sideChainService) continue;
                    Logger.Info($"Side chain {sideChainService.ChainId} transfer to {receiveSideChain.ChainId}");

                    var rawTxInfos = new List<CrossChainTransactionInfo>();
                    // from side chain
                    foreach (var transferSideAccount in AccountList[sideChainService.ChainId])
                    {
                        // to side chain account
                        foreach (var receiveSideAccount in AccountList[receiveSideChain.ChainId])
                        {
                            var rawTxInfo = CrossChainTransferWithTxId(sideChainService, symbol, transferSideAccount,
                                receiveSideAccount, receiveSideChain.ChainId, 100);
                            if (rawTxInfo == null) continue;

                            Thread.Sleep(100);
                            rawTxInfos.Add(rawTxInfo);
                        }
                    }

                    Logger.Info($"Check cross chain transfer result on chain {sideChainService.ChainId}:");
                    foreach (var rawTxInfo in rawTxInfos)
                    {
                        var resultTxInfo = GetCrossChainTransferResult(sideChainService, rawTxInfo);
                        if (resultTxInfo == null) continue;
                        sideResultTxInfos.Add(resultTxInfo);
                        Logger.Info(
                            $"The transactions block is:{resultTxInfo.BlockHeight},transaction id is: {resultTxInfo.TxId}");
                    }

                    sideRawTxInfos.Add(receiveSideChain.ChainId, sideResultTxInfos);
                }

                Logger.Info("Waiting for the index");
                Thread.Sleep(200000);

                var mainChainReceiveTxIds = new List<CrossChainTransactionInfo>();
                Logger.Info($"Main chain received Token {symbol}");
                foreach (var mainRawTxInfo in mainResultTxInfos)
                {
                    Logger.Info("Check the index:");
                    while (!CheckSideChainBlockIndex(sideChainService,mainRawTxInfo))
                    {
                        Logger.Info("Block is not recorded ");
                        Thread.Sleep(10000);
                    }
                    
                    Logger.Info($"Receive CrossTransfer Transaction id is :{mainRawTxInfo.TxId}");
                    var crossChainReceiveTokenInput = ReceiveFromSideChainInput(sideChainService, mainRawTxInfo);
                    MainChainService.TokenService.SetAccount(mainRawTxInfo.FromAccount);
                    var txId = MainChainService.TokenService.ExecuteMethodWithTxId(TokenMethod.CrossChainReceiveToken,
                        crossChainReceiveTokenInput);

                    var txInfo = new CrossChainTransactionInfo(txId, mainRawTxInfo.ReceiveAccount);
                    mainChainReceiveTxIds.Add(txInfo);
                }
                
                Logger.Info($"Check cross chain receive result on chain {MainChainService.ChainId}");
                var mainResultList = CheckoutTransferResult(MainChainService, mainChainReceiveTxIds);
                Logger.Error("Receive Failed transaction:");
                foreach (var result in mainResultList[TransactionResultStatus.Failed])
                {
                    Logger.Info(result.TxId);
                }

                Logger.Info("Receive Mined transaction:");
                foreach (var result in mainResultList[TransactionResultStatus.Mined])
                {
                    Logger.Info(result.TxId);
                }
                

                Logger.Info($"Side chain received Token {symbol}");
                foreach (var receiveSideChain in SideChainServices)
                {
                    Logger.Info($"Side chain {receiveSideChain.ChainId} receive token {symbol}");
                    if (receiveSideChain == sideChainService) continue;
                    var sideChainReceiveTxIds = new List<CrossChainTransactionInfo>();
                    foreach (var sideRawTxInfo in sideRawTxInfos[receiveSideChain.ChainId])
                    {
                        Logger.Info($"Receive CrossTransfer Transaction id is :{sideRawTxInfo.TxId}");

                        var crossChainReceiveTokenInput = ReceiveFromSideChainInput(sideChainService, sideRawTxInfo);
                        receiveSideChain.TokenService.SetAccount(sideRawTxInfo.FromAccount);
                        var txId = receiveSideChain.TokenService.ExecuteMethodWithTxId(
                            TokenMethod.CrossChainReceiveToken,
                            crossChainReceiveTokenInput);

                        var txInfo = new CrossChainTransactionInfo(txId, sideRawTxInfo.ReceiveAccount);
                        sideChainReceiveTxIds.Add(txInfo);
                    }
                    
                    Logger.Info($"Check cross chain receive result on chain {receiveSideChain.ChainId}:");
                    var sideResultList = CheckoutTransferResult(receiveSideChain, sideChainReceiveTxIds);
                    Logger.Error("Receive Failed transaction:");
                    foreach (var result in sideResultList[TransactionResultStatus.Failed])
                    {
                        Logger.Info(result.TxId);
                    }

                    Logger.Info("Receive Mined transaction:");
                    foreach (var result in sideResultList[TransactionResultStatus.Mined])
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
                foreach (var sideChain in SideChainServices)
                {
                    foreach (var sideAccount in AccountList[sideChain.ChainId])
                    {
                        var accountBalance = GetBalance(sideChain, sideAccount, symbol);
                        Logger.Info($"Account:{sideAccount}, {symbol} balance is: {accountBalance}");
                    }
                }
            }
        }
    }
}