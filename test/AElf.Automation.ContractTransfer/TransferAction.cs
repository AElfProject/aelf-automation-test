using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TransferWrapperContract;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using log4net;
using Shouldly;

namespace AElf.Automation.ContractTransfer
{
    public class TransferAction : BasicAction
    {
        public TransferAction()
        {
            GetService();
        }
        
        public List<TransferWrapperContract> DeployWrapperContractWithAuthority(out TokenContract Token)
        {
            var list = new List<TransferWrapperContract>();
            var tokenAddress =  AuthorityManager.DeployContract(InitAccount,
                    "AElf.Contracts.MultiToken", Password);
                Token = new TokenContract(NodeManager,InitAccount,tokenAddress.ToBase58());
                
                while (list.Count != ContractCount)
                {
                    var contractAddress =
                        AuthorityManager.DeployContract(InitAccount,
                            "AElf.Contracts.TransferWrapperContract", Password);
                    if (contractAddress.Equals(null))
                        continue;
                    var wrapperContract =
                        new TransferWrapperContract(NodeManager, InitAccount, contractAddress.ToBase58());
                    var initialize = wrapperContract.Initialize(tokenAddress, InitAccount, Password);
                    initialize.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                    list.Add(wrapperContract);
                }

                return list;
        }
        
        public Dictionary<TransferWrapperContract, string> CreateAndIssueTokenForWrapper(
            IEnumerable<TransferWrapperContract> contracts, TokenContract token)
        {
            var tokenList = new Dictionary<TransferWrapperContract, string>();
            var transferWrapperContracts = contracts as TransferWrapperContract[] ?? contracts.ToArray();
            foreach (var contract in transferWrapperContracts)
            {
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

                var issueToken = token.IssueBalance(InitAccount, contract.ContractAddress, 10_0000_0000_00000000, symbol);
                issueToken.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var balance = token.GetUserBalance(contract.ContractAddress, symbol);
                balance.ShouldBe(10_0000_0000_00000000);

                tokenList.Add(contract, symbol);
            }

            return tokenList;
        }
        
        public void ContinueContractTransfer(Dictionary<TransferWrapperContract, string> tokenInfo, CancellationTokenSource cts,
            CancellationToken token)
        {
            try
            {
                for (var r = 1; r > 0; r++) //continuous running
                    try
                    {
                        Logger.Info("Execution transaction request round: {0}", r);
                        var txsTasks = new List<Task>();
                        //multi task for SendTransactions query
                        foreach (var (contract, symbol) in tokenInfo)
                        {
                            txsTasks.Add(Task.Run(() => ThroughContractTransfer(contract, symbol), token));
                        }
                        Task.WaitAll(txsTasks.ToArray<Task>());
                    }
                    catch (AggregateException exception)
                    {
                        Logger.Error($"Request to {NodeManager.GetApiUrl()} got exception, {exception}");
                    }
                    catch (Exception e)
                    {
                        var message = "Execute continuous transaction got exception." +
                                      $"\r\nMessage: {e.Message}" +
                                      $"\r\nStackTrace: {e.StackTrace}";
                        Logger.Error(message);
                    }
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                Logger.Error("Cancel all tasks due to transaction execution exception.");
                cts.Cancel(); //cancel all tasks
            }
        }

        private void ThroughContractTransfer(TransferWrapperContract contract, string symbol)
        {
            var rawTransactionList = new ConcurrentBag<string>();

            Logger.Info($"ContractTransfer");
            var count = TransactionCount / TransactionGroup;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            Parallel.For(1, TransactionGroup + 1, item =>
            {
                var (from, to) = GetTransferPair(item - 1);
                for (var i = 0; i < count; i++)
                {
                    var transferInput = new ThroughContractTransferInput
                    {
                        Symbol = symbol,
                        To = to.ConvertAddress(),
                        Amount = 1,
                        Memo = $"T - {Guid.NewGuid()}"
                    };
                    var requestInfo =
                        NodeManager.GenerateRawTransaction(from, contract.ContractAddress,
                            TransferWrapperMethod.ContractTransfer.ToString(),
                            transferInput);
                    rawTransactionList.Add(requestInfo);
                }
            });
            
            stopwatch.Stop();
            var createTxsTime = stopwatch.ElapsedMilliseconds;

            // contract.CheckTransactionResultList();

            var rawTransactions = string.Join(",", rawTransactionList);
            var transactions = NodeManager.SendTransactions(rawTransactions);
            // Logger.Info(transactions);

            Thread.Sleep(100);
            var requestTxsTime = stopwatch.ElapsedMilliseconds;
            Logger.Info(
                $"Thread {contract.ContractAddress}-{symbol} request transactions: " +
                $"{TransactionCount}, create time: {createTxsTime}ms, request time: {requestTxsTime}ms.");
        }
        
        private static readonly ILog Logger = Log4NetHelper.GetLogger();

    }
    
}