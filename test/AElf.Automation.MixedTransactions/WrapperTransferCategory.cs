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
            SystemToken = ContractManager.Token;
        }

        public void PrepareWrapperTransfer(Dictionary<TransferWrapperContract, string> tokenInfo)
        {
            foreach (var (contract, symbol) in tokenInfo)
            {
                var virtualAccountList = GetFromVirtualAccounts(contract);
                foreach (var account in virtualAccountList)
                {
                    var balance = SystemToken.GetUserBalance(account, symbol);
                    if (balance >=1000_00000000)
                        continue;
                    SystemToken.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                    {
                        To = account.ConvertAddress(),
                        Amount = 1000000_00000000,
                        Symbol = symbol,
                        Memo = $"T-{Guid.NewGuid()}"
                    });
                }
            }
        }

        public Dictionary<TransferWrapperContract, string> CreateAndIssueTokenForWrapper(
            IEnumerable<TransferWrapperContract> contracts)
        {
            var systemToken = ContractManager.Token;
            var tokenList = new Dictionary<TransferWrapperContract, string>();
            foreach (var contract in contracts)
            {
                var symbol = GenerateNotExistTokenSymbol(systemToken);
                var transaction = systemToken.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
                {
                    Symbol = symbol,
                    TokenName = $"elf token {symbol}",
                    TotalSupply = 10_0000_0000_00000000L,
                    Decimals = 8,
                    Issuer = InitAccount.ConvertAddress(),
                    IsBurnable = true
                });
                transaction.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                var issueToken = systemToken.IssueBalance(InitAccount, InitAccount, 10_0000_0000_00000000, symbol);
                issueToken.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
                var balance = systemToken.GetUserBalance(InitAccount, symbol);
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


        public List<TransferWrapperContract> DeployWrapperContractWithAuthority()
        {
            var list = new List<TransferWrapperContract>();
            var wrapperContractInfo = ContractInfos.Find(info => info.ContractName.Equals("Wrapper"));
            if (!wrapperContractInfo.IsNeedDeploy)
            {
                var wrapperAddress = wrapperContractInfo.TokenInfos;
                list.AddRange(wrapperAddress.Select(wrapper =>
                    new TransferWrapperContract(NodeManager, InitAccount, wrapper.ContractAddress)));
            }
            else
            {
                while (list.Count != wrapperContractInfo.ContractCount)
                {
                    var contractAddress =
                        AuthorityManager.DeployContractWithAuthority(InitAccount,
                            "AElf.Contracts.TransferWrapperContract", Password);
                    if (contractAddress.Equals(null))
                        continue;
                    var wrapperContract =
                        new TransferWrapperContract(NodeManager, InitAccount, contractAddress.ToBase58());
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
                        Logger.Info("Execution transaction request round: {0}", r);

                        //multi task for SendTransactions query
                        var txsTasks = new List<Task>();
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
            Logger.Info(transactions);

            Thread.Sleep(1000);
        }

        private TokenContract SystemToken { get; }
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
    }
}