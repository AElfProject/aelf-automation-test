using System;
using System.Collections.Generic;
using System.Threading;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using Shouldly;

namespace AElf.Automation.SideChain.Verification.CrossChainTransfer
{
    public class CrossChainTransferPrepare : CrossChainBase
    {
        private const long amount = 10000_00000000;

        public CrossChainTransferPrepare()
        {
            MainChainService = InitMainChainServices();
            SideChainServices = InitSideChainServices();
        }

        public void DoCrossChainTransferPrepare()
        {
            Logger.Info("Prepare test account:");
            PrimaryTokens = new List<string>();
            CreateTester(Count);
            TransferToInitAccount(NativeToken);
            SideTransferToInitAccount();
            InitCrossChainTransfer();
            if (TokenSymbols.Count == 0) return;
            OtherTransferPerPare();
        }

        private void TransferToInitAccount(string symbol)
        {
            var initRawTxInfos = new Dictionary<int, CrossChainTransactionInfo>();
            foreach (var sideChainService in SideChainServices)
            {
                var balance = sideChainService.TokenService.GetUserBalance(InitAccount, symbol);
                if (balance >= amount * Count)
                {
                    Logger.Info(
                        $"Side chain {sideChainService.ChainId} account {sideChainService.CallAddress}" +
                        $"{symbol} token balance is {balance}");
                    continue;
                }

                var rawTxInfo = CrossChainTransferWithResult(MainChainService, symbol, InitAccount, InitAccount,
                    sideChainService.ChainId, amount * Count);
                initRawTxInfos.Add(sideChainService.ChainId, rawTxInfo);
                Logger.Info(
                    $"the transactions block is:{rawTxInfo.BlockHeight},transaction id is: {rawTxInfo.TxId}");
            }

            if (initRawTxInfos.Count == 0) return;
            Logger.Info("Waiting for the index");
            Thread.Sleep(30000);

            foreach (var sideChainService in SideChainServices)
            {
                Logger.Info($"Side chain {sideChainService.ChainId} received token");
                Logger.Info(
                    $"Receive CrossTransfer Transaction id is : {initRawTxInfos[sideChainService.ChainId].TxId}");
                Logger.Info("Check the index:");
                while (!CheckSideChainBlockIndex(sideChainService, initRawTxInfos[sideChainService.ChainId]))
                {
                    Console.WriteLine("Block is not recorded ");
                    Thread.Sleep(10000);
                }

                var input = ReceiveFromMainChainInput(initRawTxInfos[sideChainService.ChainId]);
                sideChainService.TokenService.SetAccount(InitAccount);
                var result = sideChainService.TokenService.ExecuteMethodWithResult(
                    TokenMethod.CrossChainReceiveToken,
                    input);
                result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                Logger.Info($"check the balance on the side chain {sideChainService.ChainId}");
                var accountBalance = sideChainService.TokenService.GetUserBalance(InitAccount, symbol);
                Logger.Info(
                    $"On side chain {sideChainService.ChainId}, InitAccount:{InitAccount}, {symbol} balance is {accountBalance}");
            }
        }

        private void SideTransferToInitAccount()
        {
            foreach (var sideChainService in SideChainServices)
            {
                var initRawTxInfos = new Dictionary<int, CrossChainTransactionInfo>();
                var symbol = sideChainService.PrimaryTokenSymbol;
                var mainBalance = MainChainService.TokenService.GetUserBalance(InitAccount, symbol);
                if (mainBalance <= amount * Count)
                {
                    var rawTxInfo = CrossChainTransferWithResult(sideChainService, symbol, InitAccount, InitAccount,
                        MainChainService.ChainId, amount * Count);
                    initRawTxInfos.Add(MainChainService.ChainId, rawTxInfo);
                    Logger.Info(
                        $"the transactions block is:{rawTxInfo.BlockHeight},transaction id is: {rawTxInfo.TxId}");
                }

                foreach (var sideChain in SideChainServices)
                {
                    if (sideChain.Equals(sideChainService)) continue;
                    var balance = sideChain.TokenService.GetUserBalance(InitAccount, symbol);
                    if (balance > amount * Count) continue;
                    var rawTxInfo = CrossChainTransferWithResult(sideChainService, symbol, InitAccount, InitAccount,
                        sideChain.ChainId, amount * Count);
                    initRawTxInfos.Add(sideChain.ChainId, rawTxInfo);
                    Logger.Info(
                        $"the transactions block is:{rawTxInfo.BlockHeight},transaction id is: {rawTxInfo.TxId}");
                }

                if (initRawTxInfos.Count == 0) return;
                Logger.Info("Waiting for the index");
                Thread.Sleep(60000);

                foreach (var initRawTxInfo in initRawTxInfos)
                    if (initRawTxInfo.Key.Equals(MainChainService.ChainId))
                    {
                        //main receive:
                        var mainRawTxInfo = initRawTxInfo.Value;
                        MainChainCheckSideChainBlockIndex(sideChainService, mainRawTxInfo.BlockHeight);
                        var crossChainReceiveTokenInput = ReceiveFromSideChainInput(sideChainService, mainRawTxInfo);
                        MainChainService.TokenService.SetAccount(mainRawTxInfo.FromAccount);
                        var result = MainChainService.TokenService.ExecuteMethodWithResult(
                            TokenMethod.CrossChainReceiveToken,
                            crossChainReceiveTokenInput);
                        result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                        Logger.Info("check the balance on the main chain:");
                        var accountBalance = MainChainService.TokenService.GetUserBalance(InitAccount, symbol);
                        Logger.Info(
                            $"On main chain, InitAccount:{InitAccount}, {symbol} balance is {accountBalance}");
                    }
                    else
                    {
                        //side chain receive 

                        var receive = SideChainServices.Find(s => s.ChainId.Equals(initRawTxInfo.Key));
                        var rawTxInfo = initRawTxInfo.Value;
                        var mainHeight = MainChainCheckSideChainBlockIndex(sideChainService, rawTxInfo.BlockHeight);
                        while (mainHeight > GetIndexParentHeight(receive))
                        {
                            Console.WriteLine("Block is not recorded ");
                            Thread.Sleep(10000);
                        }

                        var crossChainSideReceiveTokenInput =
                            ReceiveFromSideChainInput(sideChainService, rawTxInfo);
                        receive.TokenService.SetAccount(rawTxInfo.FromAccount);
                        var sideResult = receive.TokenService.ExecuteMethodWithResult(
                            TokenMethod.CrossChainReceiveToken,
                            crossChainSideReceiveTokenInput);
                        sideResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                        Logger.Info($"check the balance on the side chain {receive.ChainId}:");
                        var balance = receive.TokenService.GetUserBalance(InitAccount, symbol);
                        Logger.Info(
                            $"On side chain {receive.ChainId}, InitAccount:{InitAccount}, {symbol} balance is {balance}");
                    }
            }
        }

        private void InitCrossChainTransfer()
        {
            Logger.Info("Transfer main chain token: ");
            PrimaryTokens.Add(NativeToken);
            foreach (var sideChain in SideChainServices) PrimaryTokens.Add(sideChain.PrimaryTokenSymbol);

            foreach (var token in PrimaryTokens)
            {
                Logger.Info($"Transfer {token} token to each account on main chain:");
                foreach (var account in AccountList)
                    MainChainService.TokenService.TransferBalance(InitAccount, account, amount, token);

                foreach (var sideChainService in SideChainServices)
                {
                    Logger.Info($"Transfer {token} token to each account on side chain {sideChainService.ChainId}:");
                    foreach (var account in AccountList)
                        if (sideChainService.PrimaryTokenSymbol.Equals(token) && IsSupplyAllToken(sideChainService))
                            IssueSideChainToken(sideChainService, account);
                        else
                            sideChainService.TokenService.TransferBalance(InitAccount, account, amount, token);
                }
            }

            foreach (var token in PrimaryTokens) CheckAccountBalance(token);
        }

        private void OtherTransferPerPare()
        {
            foreach (var symbol in TokenSymbols)
            {
                TransferToInitAccount(symbol);
                Logger.Info($"Transfer {symbol} token to each account on main chain:");
                foreach (var account in AccountList)
                    MainChainService.TokenService.TransferBalance(InitAccount, account, amount, symbol);
            }

            foreach (var symbol in TokenSymbols) CheckAccountBalance(symbol);
        }

        private void CreateTester(int count)
        {
            AccountList = new List<string>();
            Logger.Info("Create account on main chain:");
            AccountList = EnvCheck.GenerateOrGetTestUsers(count, MainChainService.NodeManager.GetApiUrl());
            // Unlock account on main chain 
            UnlockAccounts(MainChainService, AccountList);
            // Unlock account on side chain
            foreach (var sideChainService in SideChainServices) UnlockAccounts(sideChainService, AccountList);
        }
    }
}