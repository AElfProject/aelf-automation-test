using System;
using System.Collections.Generic;
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

namespace AElf.Automation.MixedTransactions
{
    public class WrapperTransferCategory : BasicCategory
    {
        public WrapperTransferCategory()
        {
            GetService();
        }

        public void PrepareWrapperTransfer(Dictionary<TransferWrapperContract, string> tokenInfo,TokenContract token)
        {
            foreach (var (contract, symbol) in tokenInfo)
            {
                var virtualAccountList = GetFromVirtualAccounts(contract);
                foreach (var account in virtualAccountList)
                {
                    var balance = token.GetUserBalance(account, symbol);
                    if (balance >=1000_00000000)
                        continue;
                    token.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                    {
                        To = account.ConvertAddress(),
                        Amount = 1000000_00000000,
                        Symbol = symbol,
                        Memo = $"T-{Guid.NewGuid()}"
                    });
                }
                var contractBalance = token.GetUserBalance(contract.ContractAddress, symbol);
                if (contractBalance >=100000_00000000)
                    return;
                
                token.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                {
                    To = contract.Contract,
                    Amount = 10000000_00000000,
                    Symbol = symbol,
                    Memo = $"T-{Guid.NewGuid()}"
                });
                token.CheckTransactionResultList();
            }
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

                var issueToken = token.IssueBalance(InitAccount, InitAccount, 10_0000_0000_00000000, symbol);
                issueToken.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var balance = token.GetUserBalance(InitAccount, symbol);
                balance.ShouldBe(10_0000_0000_00000000);

                tokenList.Add(contract, symbol);
            }

            return tokenList;
        }

        public Dictionary<TransferWrapperContract, string> GetTokenList(IEnumerable<TransferWrapperContract> contracts)
        {
            var wrapperContractInfo = ContractInfos.Find(info => info.ContractName.Equals("Wrapper"));
            var tokenList = new Dictionary<TransferWrapperContract, string>();
            foreach (var tokenInfo in wrapperContractInfo.TokenInfos)
            {
                var wrapperContracts = contracts as TransferWrapperContract[] ?? contracts.ToArray();
                var contract = wrapperContracts.First(o => o.ContractAddress.Equals(tokenInfo.ContractAddress));
                tokenList.Add(contract, tokenInfo.TokenSymbol);
            }

            return tokenList;
        }


        public List<TransferWrapperContract> DeployWrapperContractWithAuthority(out TokenContract Token)
        {
            var list = new List<TransferWrapperContract>();
            var wrapperContractInfo = ContractInfos.Find(info => info.ContractName.Equals("Wrapper"));
            if (!wrapperContractInfo.IsNeedDeploy)
            {
                var wrapperAddress = wrapperContractInfo.TokenInfos;
                list.AddRange(wrapperAddress.Select(wrapper =>
                    new TransferWrapperContract(NodeManager, InitAccount, wrapper.ContractAddress)));
                Token = GetWrapperTokenContract(list.First());
            }
            else
            {
                var tokenAddress =  AuthorityManager.DeployContract(InitAccount,
                    "AElf.Contracts.MultiToken", Password);
                Token = new TokenContract(NodeManager,InitAccount,tokenAddress.ToBase58());
                
                while (list.Count != wrapperContractInfo.ContractCount)
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
            }

            return list;
        }

        public void ContinueTransfer(Dictionary<TransferWrapperContract, string> tokenInfo, CancellationTokenSource cts,
            CancellationToken token)
        {
            try
            {
                for (var r = 1; r > 0; r++) //continuous running
                    try
                    {
                        Logger.Info("Execution virtual transfer transaction request round: {0}", r);

                        //multi task for SendTransactions query
                        var txsTasks = new List<Task>();
                        foreach (var (contract, symbol) in tokenInfo)
                        {
                            txsTasks.Add(Task.Run(() => ThroughVirtualTransfer(contract, symbol), token));
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

        public void CheckAccountAmount(Dictionary<TransferWrapperContract, string> tokenInfo, TokenContract tokenContract,CancellationTokenSource cts, 
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
                    PrepareWrapperTransfer(tokenInfo,tokenContract);
                }
                catch (Exception e)
                {
                    Logger.Error(e.Message);
                }
            }

        }

        private void ThroughVirtualTransfer(TransferWrapperContract contract, string symbol)
        {
            var rawTransactionList = new List<string>();

            for (var i = 0; i < TransactionCount; i++)
            {
                var (from, to) = GetTransferPair(i);

                var transferInput = new ThroughContractTransferInput
                {
                    Symbol = symbol,
                    To = to.ConvertAddress(),
                    Amount = ((i + 1) % 4 + 1) * 1000,
                    Memo = $"T - {Guid.NewGuid()}"
                };
                var requestInfo =
                    NodeManager.GenerateRawTransaction(from, contract.ContractAddress,
                        TransferWrapperMethod.ThroughContractTransfer.ToString(),
                        transferInput);
                rawTransactionList.Add(requestInfo);
            }

            contract.CheckTransactionResultList();


            var rawTransactions = string.Join(",", rawTransactionList);
            var transactions = NodeManager.SendTransactions(rawTransactions);
//            Logger.Info(transactions);

            Thread.Sleep(1000);
        }
        
        private void ThroughContractTransfer(TransferWrapperContract contract, string symbol)
        {
            var rawTransactionList = new List<string>();

            Logger.Info($"ContractTransfer");
            for (var i = 0; i < TransactionCount; i++)
            {
                var (from, to) = GetTransferPair(i);

                var transferInput = new ThroughContractTransferInput
                {
                    Symbol = symbol,
                    To = to.ConvertAddress(),
                    Amount = ((i + 1) % 4 + 1) * 1000,
                    Memo = $"T - {Guid.NewGuid()}"
                };
                var requestInfo =
                    NodeManager.GenerateRawTransaction(from, contract.ContractAddress,
                        TransferWrapperMethod.ContractTransfer.ToString(),
                        transferInput);
                rawTransactionList.Add(requestInfo);
            }

            contract.CheckTransactionResultList();


            var rawTransactions = string.Join(",", rawTransactionList);
            var transactions = NodeManager.SendTransactions(rawTransactions);
            Logger.Info(transactions);

            Thread.Sleep(1000);
        }

        private TokenContract GetWrapperTokenContract(TransferWrapperContract contract)
        {
            var address =  contract.GetTokenAddress();
            return new TokenContract(NodeManager,InitAccount,address.ToBase58());
        }

        private static readonly ILog Logger = Log4NetHelper.GetLogger();
    }
}