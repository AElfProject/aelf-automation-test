using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using log4net;
using Shouldly;

namespace AElf.Automation.MixedTransactions
{
    public class TransferCategory : BasicCategory
    {
        public TransferCategory()
        {
            GetService();
        }

        public void ContinueTransfer(Dictionary<TokenContract, string> tokenInfo, CancellationTokenSource cts,
            CancellationToken token)
        {
            try
            {
                for (var r = 1; r > 0; r++) //continuous running
                    try
                    {
                        Logger.Info("Execution transaction request round: {0}", r);

                        //multi task for SendTransactions query
                        var txsTasks = new List<Task>();
                        foreach (var (contract,symbol) in tokenInfo)
                        {
                            txsTasks.Add(Task.Run(() => TransferAction(contract,symbol), token));
                        }

                        Task.WaitAll(txsTasks.ToArray<Task>());
                        Thread.Sleep(1000);
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
        
        public void CheckAccountAmount(Dictionary<TokenContract, string> tokenInfo,CancellationTokenSource cts,
            CancellationToken token)
        {
            var checkRound = 1;
            while (true)
            {
                if (cts.IsCancellationRequested)
                {
                    Logger.Warn("ExecuteTokenCheckTask was been cancelled.");
                    break;
                }

                Thread.Sleep(3 * 60 * 1000);
                try
                {
                    Logger.Info($"Start check tester token balance job round: {checkRound++}");
                    PrepareTokenTransfer(tokenInfo);
                }
                catch (Exception e)
                {
                    Logger.Error(e.Message);
                }
            }
        }

        private void TransferAction(TokenContract contract, string symbol)
        {
            var rawTransactionList = new List<string>();
            for (var i = 0; i < TransactionCount; i++)
            {
                var (from, to) = GetTransferPair(i);
                var transferInput = new TransferInput
                {
                    Symbol = symbol,
                    To = to.ConvertAddress(),
                    Amount = ((i + 1) % 4 + 1) * 1000,
                    Memo = $"T - {Guid.NewGuid()}"
                };
                var requestInfo =
                    NodeManager.GenerateRawTransaction(from, contract.ContractAddress,
                        TokenMethod.Transfer.ToString(),
                        transferInput);
                rawTransactionList.Add(requestInfo);
            }

            contract.CheckTransactionResultList();

            var rawTransactions = string.Join(",", rawTransactionList);
            var transactions = NodeManager.SendTransactions(rawTransactions);
            Logger.Info(transactions);

            Thread.Sleep(1000);
        }

        public void PrepareTokenTransfer(Dictionary<TokenContract, string> tokenInfo)
        {
            foreach (var (contract, symbol) in tokenInfo)
            {
                contract.SetAccount(InitAccount);
                foreach (var account in FromAccountList)
                {
                    if (account == InitAccount) continue;
                    var balance = contract.GetUserBalance(account, symbol);
                    if (balance >=1000_00000000)
                        continue;
                    contract.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                    {
                        To = account.ConvertAddress(),
                        Amount = 1000000_00000000,
                        Symbol = symbol,
                        Memo = $"T-{Guid.NewGuid()}"
                    });
                }

                contract.CheckTransactionResultList();
            }
        }

        public List<TokenContract> DeployTokenContractWithAuthority()
        {
            var list = new List<TokenContract>();
            var tokenContractInfo = ContractInfos.Find(info => info.ContractName.Equals("Token"));
            if (!tokenContractInfo.IsNeedDeploy)
            {
                var tokenAddress = tokenContractInfo.TokenInfos;
                list.AddRange(tokenAddress.Select(token =>
                    new TokenContract(NodeManager, InitAccount, token.ContractAddress)));
            }
            else
            {
                while (list.Count != tokenContractInfo.ContractCount)
                {
                    var contractAddress =
                        AuthorityManager.DeployContract(InitAccount, "AElf.Contracts.MultiToken",
                            Password);
                    if (contractAddress.Equals(null))
                        continue;
                    var tokenContract = new TokenContract(NodeManager, InitAccount, contractAddress.ToBase58());
                    list.Add(tokenContract);
                }
            }

            return list;
        }

        public Dictionary<TokenContract, string> CreateAndIssueTokenForToken(IEnumerable<TokenContract> contracts)
        {
            var tokenList = new Dictionary<TokenContract, string>();
            foreach (var contract in contracts)
            {
                var symbol = GenerateNotExistTokenSymbol(contract);

                var transaction = contract.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
                {
                    Symbol = symbol,
                    TokenName = $"elf token {symbol}",
                    TotalSupply = 10_0000_0000_00000000L,
                    Decimals = 8,
                    Issuer = InitAccount.ConvertAddress(),
                    IsBurnable = true
                });
                transaction.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                var issueToken = contract.IssueBalance(InitAccount, InitAccount, 10_0000_0000_00000000, symbol);
                issueToken.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var balance = contract.GetUserBalance(InitAccount, symbol);
                balance.ShouldBe(10_0000_0000_00000000);
                tokenList.Add(contract, symbol);
            }

            return tokenList;
        }

        public Dictionary<TokenContract, string> GetTokenList(IEnumerable<TokenContract> contracts)
        {
            var tokenContractInfo = ContractInfos.Find(info => info.ContractName.Equals("Token"));
            var tokenList = new Dictionary<TokenContract, string>();
            foreach (var tokenInfo in tokenContractInfo.TokenInfos)
            {
                var tokenContracts = contracts as TokenContract[] ?? contracts.ToArray();
                var contract = tokenContracts.First(o => o.ContractAddress.Equals(tokenInfo.ContractAddress));
                tokenList.Add(contract, tokenInfo.TokenSymbol);
            }

            return tokenList;
        }

        private static readonly ILog Logger = Log4NetHelper.GetLogger();
    }
}