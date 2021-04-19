using System;
using System.Collections.Generic;
using System.Diagnostics;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TransferWrapperContract;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using log4net;
using Shouldly;
using Volo.Abp.Threading;

namespace AElf.Automation.BasicTransaction
{
    public class TransactionAction: BasicAction
    {
        public TransactionAction()
        {
            GetService();
        }
        
        public TransferWrapperContract DeployWrapperContract(TokenContract tokenAddress)
        {
            if (WrapperAddress != "")
                return new TransferWrapperContract(NodeManager, InitAccount, WrapperAddress);
            var contractAddress =
                AuthorityManager.DeployContract(InitAccount,
                    "AElf.Contracts.TransferWrapperContract", Password);
            var wrapperContract =
                new TransferWrapperContract(NodeManager, InitAccount, contractAddress.ToBase58());
            var initialize = wrapperContract.Initialize(tokenAddress.Contract, InitAccount, Password);
            initialize.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            Logger.Info($"Wrapper Address: {wrapperContract.ContractAddress}");

            return wrapperContract;
        }
        
        public TokenContract DeployTokenContract(TestMode mode)
        {
            if (mode == TestMode.RandomContractTransfer)
                TokenAddress = "";
            
            if (TokenAddress != "")
                return new TokenContract(NodeManager, InitAccount, TokenAddress);
            var tokenAddress = AuthorityManager.DeployContract(InitAccount,
                "AElf.Contracts.MultiToken", Password);
            var token = new TokenContract(NodeManager, InitAccount, tokenAddress.ToBase58());
            Logger.Info($"Token Address: {token.ContractAddress}");
            return token;
        }

        public string CreateAndIssueTokenForWrapper(Address tokenAddress)
        {
            var token = new TokenContract(NodeManager, InitAccount, tokenAddress.ToBase58());
            var symbol = GenerateNotExistTokenSymbol(token);
            var transaction = token.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
            {
                Symbol = symbol,
                TokenName = $"elf token {symbol}",
                TotalSupply = 10_0000_0000_00000000L,
                Decimals = 8,
                Issuer = InitAccount.ConvertAddress(),
                IsBurnable = true
            });
            transaction.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var issueToken = token.IssueBalance(InitAccount, InitAccount, 10_0000_0000_00000000, symbol);
            issueToken.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var balance = token.GetUserBalance(InitAccount, symbol);
            balance.ShouldBe(10_0000_0000_00000000);
            return symbol;
        }
        
        public long TransferFromAccount(TokenContract token, string symbol)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            token.ExecuteMethodWithTxId(TokenMethod.Transfer,new TransferInput
            {
                To = TestAccount.ConvertAddress(),
                Amount = TransferAmount,
                Symbol = symbol,
                Memo = $"T-{Guid.NewGuid().ToString()}"
            });
            stopwatch.Stop();
            var checkTime = stopwatch.ElapsedMilliseconds;
            return checkTime;
        }

        public long CheckAccount(TokenContract token, string symbol)
        {
            long all = 0;
            var txIds = new List<string>();
            foreach (var account in TestAccountList)
            {
                var id =token.ExecuteMethodWithTxId(TokenMethod.Transfer,new TransferInput
                {
                    To = account.ConvertAddress(),
                    Amount = TransferAmount,
                    Symbol = symbol,
                    Memo = $"T-{Guid.NewGuid().ToString()}"
                });
                txIds.Add(id);
            }
            NodeManager.CheckTransactionListResult(txIds);


            foreach (var account in TestAccountList)
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var balance = token.GetUserBalance(account,symbol);
                stopwatch.Stop();
                var checkTime = stopwatch.ElapsedMilliseconds;
                all += checkTime;
                Logger.Info($"account {account} balance is {balance}");
            }
            return all;
        }
        
        public long CheckAccountBalance(TokenContract token, string symbol)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var balance = token.GetUserBalance(InitAccount,symbol);
            var testBalance = token.GetUserBalance(TestAccount, symbol);
            stopwatch.Stop();
            
            var result = token.TransferBalance(InitAccount, TestAccount, TransferAmount, symbol);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            stopwatch.Restart();
            var afterBalance = token.GetUserBalance(InitAccount,symbol);
            var afterTestBalance = token.GetUserBalance(TestAccount,symbol);
            stopwatch.Stop();

            var checkTime = stopwatch.ElapsedMilliseconds;
            afterBalance.ShouldBe(balance - TransferAmount);
            afterTestBalance.ShouldBe(testBalance + TransferAmount);
            Logger.Info($"Before transfer from account balance is {balance}, to account balance is {testBalance}");
            Logger.Info($"After transfer from account balance is {afterBalance}, to account balance is {afterTestBalance}");
            return checkTime;
        }
        
        public long TransferFromContract(TokenContract token, TransferWrapperContract wrapper, string symbol)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            wrapper.ExecuteMethodWithTxId(TransferWrapperMethod.ContractTransfer,
                new ThroughContractTransferInput
                {
                    Symbol = symbol,
                    To = TestAccount.ConvertAddress(),
                    Amount = TransferAmount
                });
            stopwatch.Stop();
            var checkTime = stopwatch.ElapsedMilliseconds;
            return checkTime;
        }

        public long CheckContract(TokenContract token, TransferWrapperContract wrapper, string symbol)
        {
            var result = token.TransferBalance(InitAccount, wrapper.ContractAddress, TransferAmount * 2, symbol);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var balance = token.GetUserBalance(wrapper.ContractAddress, symbol);
            var testBalance = token.GetUserBalance(TestAccount, symbol);
            stopwatch.Stop();

            var txResult = wrapper.ExecuteMethodWithResult(TransferWrapperMethod.ContractTransfer,
                new ThroughContractTransferInput
                {
                    Symbol = symbol,
                    To = TestAccount.ConvertAddress(),
                    Amount = TransferAmount
                });
            txResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var afterBalance = token.GetUserBalance(wrapper.ContractAddress, symbol);
            var afterTestBalance = token.GetUserBalance(TestAccount, symbol);
            
            stopwatch.Restart();
            afterBalance.ShouldBe(balance - TransferAmount);
            afterTestBalance.ShouldBe(testBalance + TransferAmount);
            stopwatch.Stop();
            
            Logger.Info($"Before transfer from account balance is {balance}, to account balance is {testBalance}");
            Logger.Info($"After transfer from account balance is {afterBalance}, to account balance is {afterTestBalance}");
            
            var checkTime = stopwatch.ElapsedMilliseconds;
            return checkTime;
        }

        public long CheckBlockHeight(long count)
        {
            long all = 0;
            var height = AsyncHelper.RunSync(() => NodeManager.ApiClient.GetBlockHeightAsync());
            var startHeight = height - count;
            Logger.Info($"Current block height: {height}, check block info from {startHeight} to {height-1}");
            for (var i = startHeight; i < height; i++)
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var info = AsyncHelper.RunSync(() => NodeManager.ApiClient.GetBlockByHeightAsync(i));
                stopwatch.Stop();
                Logger.Info($"block {i},hash : {info.BlockHash}");
                var checkTime = stopwatch.ElapsedMilliseconds;
                all += checkTime;
            }
            return all;
        }

        public void TransferFromVirtual(TokenContract token, TransferWrapperContract wrapper, string symbol)
        {
            var virtualAccount = GetFromVirtualAccounts(wrapper);
            var result = token.TransferBalance(InitAccount, virtualAccount.ToBase58(), TransferAmount * 2, symbol);
            result.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var balance = token.GetUserBalance(virtualAccount.ToBase58(), symbol);
            var testBalance = token.GetUserBalance(TestAccount, symbol);
            var txResult = wrapper.ExecuteMethodWithResult(TransferWrapperMethod.ThroughContractTransfer,
                new ThroughContractTransferInput
                {
                    Symbol = symbol,
                    To = TestAccount.ConvertAddress(),
                    Amount = TransferAmount
                });
            txResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            
            var afterBalance = token.GetUserBalance(virtualAccount.ToBase58(), symbol);
            var afterTestBalance = token.GetUserBalance(TestAccount, symbol);
            
            afterBalance.ShouldBe(balance - TransferAmount);
            afterTestBalance.ShouldBe(testBalance + TransferAmount);
            
            Logger.Info($"Before transfer from account balance is {balance}, to account balance is {testBalance}");
            Logger.Info($"After transfer from account balance is {afterBalance}, to account balance is {afterTestBalance}");
        }

        private static readonly ILog Logger = Log4NetHelper.GetLogger();
    }
}