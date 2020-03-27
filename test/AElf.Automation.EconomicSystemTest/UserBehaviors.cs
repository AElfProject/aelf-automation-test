using System;
using System.Collections.Generic;
using AElf.Client.Dto;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit;
using AElf.Contracts.TokenConverter;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using Google.Protobuf.WellKnownTypes;
using Shouldly;

namespace AElf.Automation.EconomicSystemTest
{
    public partial class Behaviors
    {
        //action
        public TransactionResultDto UserVote(string account, string candidate, int lockTime, long amount)
        {
            //check balance
            var beforeBalance = TokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance,
                new GetBalanceInput
                {
                    Owner = AddressHelper.Base58StringToAddress(account),
                    Symbol = NodeOption.NativeTokenSymbol
                }).Balance;

            ElectionService.SetAccount(account);
            var vote = ElectionService.ExecuteMethodWithResult(ElectionMethod.Vote, new VoteMinerInput
            {
                CandidatePubkey = NodeManager.GetAccountPublicKey(candidate),
                Amount = amount,
                EndTimestamp = DateTime.UtcNow.Add(TimeSpan.FromDays(lockTime)).ToTimestamp()
            });
            vote.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

            var afterBalance = TokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
            {
                Owner = AddressHelper.Base58StringToAddress(account),
                Symbol = NodeOption.NativeTokenSymbol
            }).Balance;

//            beforeBalance.ShouldBe(afterBalance + amount, "user voted but user balance not correct.");

            return vote;
        }

        public List<string> UserVoteWithTxIds(string account, string candidate, int lockTime, int times)
        {
            ElectionService.SetAccount(account);
            var list = new List<string>();
            for (var i = 1; i <= times; i++)
            {
                var txId = ElectionService.ExecuteMethodWithTxId(ElectionMethod.Vote, new VoteMinerInput
                {
                    CandidatePubkey = NodeManager.GetAccountPublicKey(candidate),
                    Amount = i,
                    EndTimestamp = DateTime.UtcNow.Add(TimeSpan.FromDays(lockTime)).ToTimestamp()
                });

                list.Add(txId);
            }

            return list;
        }

        public TransactionResultDto Profit(string account, Hash profitId)
        {
            ProfitService.SetAccount(account);
            var result = ProfitService.ExecuteMethodWithResult(ProfitMethod.ClaimProfits, new ClaimProfitsInput
            {
                SchemeId = profitId
            });

            return result;
        }

        #region TokenConverter Method

        // action
        public TransactionResultDto TokenConverterInitialize(string initAccount)
        {
            var ramConnector = new Connector
            {
                Symbol = "RAM",
                IsPurchaseEnabled = true,
                IsVirtualBalanceEnabled = false,
                VirtualBalance = 0,
                Weight = "0.5"
            };
            var cpuConnector = new Connector
            {
                Symbol = "CPU",
                IsPurchaseEnabled = true,
                IsVirtualBalanceEnabled = false,
                VirtualBalance = 0,
                Weight = "0.5"
            };
            var netConnector = new Connector
            {
                Symbol = "NET",
                IsPurchaseEnabled = true,
                IsVirtualBalanceEnabled = false,
                VirtualBalance = 0,
                Weight = "0.5"
            };
            var stoConnector = new Connector
            {
                Symbol = "DISK",
                IsPurchaseEnabled = true,
                IsVirtualBalanceEnabled = false,
                VirtualBalance = 0,
                Weight = "0.5"
            };
            var elfConnector = new Connector
            {
                Symbol = NodeOption.NativeTokenSymbol,
                IsPurchaseEnabled = true,
                IsVirtualBalanceEnabled = true,
                VirtualBalance = 100_0000,
                Weight = "0.5"
            };

            var result = TokenConverterService.ExecuteMethodWithResult(TokenConverterMethod.Initialize,
                new InitializeInput
                {
                    BaseTokenSymbol = NodeOption.NativeTokenSymbol,
                    FeeRate = "0.05",
                    Connectors = {ramConnector, cpuConnector, netConnector, stoConnector, elfConnector}
                });

            return result;
        }

        #endregion

        #region Token Method

        //token action
        public TransactionResultDto TransferToken(string from, string to, long amount, string symbol = "ELF")
        {
            TokenService.SetAccount(from);

            return TokenService.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
            {
                Symbol = symbol,
                Amount = amount,
                To = AddressHelper.Base58StringToAddress(to),
                Memo = $"transfer {from}=>{to} with amount {amount}."
            });
        }

        public TransactionResultDto CreateToken(string issuer, string symbol, string tokenName)
        {
            TokenService.SetAccount(issuer);
            var create = TokenService.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
            {
                Symbol = symbol,
                Decimals = 2,
                IsBurnable = true,
                Issuer = AddressHelper.Base58StringToAddress(issuer),
                TokenName = tokenName,
                TotalSupply = 100_0000
            });
            return create;
        }

        public TransactionResultDto IssueToken(string issuer, string symbol, string toAddress)
        {
            TokenService.SetAccount(issuer);
            var issue = TokenService.ExecuteMethodWithResult(TokenMethod.Issue, new IssueInput
            {
                Symbol = symbol,
                Amount = 100,
                Memo = "Issue",
                To = AddressHelper.Base58StringToAddress(toAddress)
            });

            return issue;
        }

        public TransactionResultDto ApproveToken(string from, string to, long amount, string symbol = "ELF")
        {
            TokenService.SetAccount(from);

            var approve = TokenService.ExecuteMethodWithResult(TokenMethod.Approve, new ApproveInput
            {
                Symbol = symbol,
                Spender = AddressHelper.Base58StringToAddress(to),
                Amount = amount
            });
            return approve;
        }

        public TransactionResultDto UnApproveToken(string from, string to, long amount, string symbol = "ELF")
        {
            TokenService.SetAccount(from);

            var unapprove = TokenService.ExecuteMethodWithResult(TokenMethod.UnApprove, new UnApproveInput
            {
                Symbol = symbol,
                Spender = AddressHelper.Base58StringToAddress(to),
                Amount = amount
            });
            return unapprove;
        }

        public TransactionResultDto TransfterFromToken(string from, string to, long amount, string symbol = "ELF")
        {
            TokenService.SetAccount(to);
            var transferFrom = TokenService.ExecuteMethodWithResult(TokenMethod.TransferFrom, new TransferFromInput
            {
                Symbol = symbol,
                From = AddressHelper.Base58StringToAddress(from),
                To = AddressHelper.Base58StringToAddress(to),
                Amount = amount,
                Memo = $"transferfrom: from {from} to {to}"
            });
            return transferFrom;
        }

        public TransactionResultDto BurnToken(long amount, string account, string symbol = "ELF")
        {
            TokenService.SetAccount(account);
            var burn = TokenService.ExecuteMethodWithResult(TokenMethod.Burn, new BurnInput
            {
                Amount = amount,
                Symbol = symbol
            });
            return burn;
        }

        #endregion
    }
}