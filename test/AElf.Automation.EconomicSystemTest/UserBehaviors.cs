using System;
using System.Collections.Generic;
using AElf.Client.Dto;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit;
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
                    Owner = account.ConvertAddress(),
                    Symbol = NodeOption.NativeTokenSymbol
                }).Balance;
            
            var beforeVoteBalance = TokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance,
                new GetBalanceInput
                {
                    Owner =  account.ConvertAddress(),
                    Symbol = "SHARE"
                }).Balance;

            ElectionService.SetAccount(account);
            var vote = ElectionService.ExecuteMethodWithResult(ElectionMethod.Vote, new VoteMinerInput
            {
                CandidatePubkey = NodeManager.GetAccountPublicKey(candidate),
                Amount = amount,
                EndTimestamp = DateTime.UtcNow.Add(TimeSpan.FromSeconds(lockTime)).ToTimestamp()
            });
            vote.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var fee = vote.GetDefaultTransactionFee();
            
            var afterBalance = TokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
            {
                Owner =  account.ConvertAddress(),
                Symbol = NodeOption.NativeTokenSymbol
            }).Balance;
            
            var afterVoteBalance = TokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
            {
                Owner =  account.ConvertAddress(),
                Symbol = "SHARE"
            }).Balance;
            
            afterBalance.ShouldBe(beforeBalance - amount - fee);
            afterVoteBalance.ShouldBe(beforeVoteBalance + amount);
//            beforeBalance.ShouldBe(afterBalance + amount, "user voted but user balance not correct.");

            return vote;
        }
        
         public TransactionResultDto UserChangeVote(string account, string candidate, Hash voteId, bool isReset)
        {
            ElectionService.SetAccount(account);
            var vote = ElectionService.ExecuteMethodWithResult(ElectionMethod.ChangeVotingOption, new ChangeVotingOptionInput
            {
                CandidatePubkey = NodeManager.GetAccountPublicKey(candidate),
                VoteId = voteId,
                IsResetVotingTime = isReset
            });
            vote.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var fee = vote.GetDefaultTransactionFee();
            return vote;
        }
         
         public TransactionResultDto UserChangeVoteOld(string account, string candidate, Hash voteId)
         {
             ElectionService.SetAccount(account);
             var vote = ElectionService.ExecuteMethodWithResult(ElectionMethod.ChangeVotingOption, new ChangeVotingOptionInput
             {
                 CandidatePubkey = NodeManager.GetAccountPublicKey(candidate),
                 VoteId = voteId,
             });
             vote.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
             var fee = vote.GetDefaultTransactionFee();
             return vote;
         }
         
        public TransactionResultDto UserWithdraw(string account, string voteId, long amount)
        {
            //check balance
            var beforeBalance = TokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance,
                new GetBalanceInput
                {
                    Owner = account.ConvertAddress(),
                    Symbol = NodeOption.NativeTokenSymbol
                }).Balance;
            
            var beforeVoteBalance = TokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance,
                new GetBalanceInput
                {
                    Owner =  account.ConvertAddress(),
                    Symbol = "SHARE"
                }).Balance;

            ElectionService.SetAccount(account);
            var withdraw = ElectionService.ExecuteMethodWithResult(ElectionMethod.Withdraw,
                    Hash.LoadFromHex(voteId));
            withdraw.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            var fee = withdraw.GetDefaultTransactionFee();
            
            var afterBalance = TokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
            {
                Owner =  account.ConvertAddress(),
                Symbol = NodeOption.NativeTokenSymbol
            }).Balance;
            
            var afterVoteBalance = TokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
            {
                Owner =  account.ConvertAddress(),
                Symbol = "SHARE"
            }).Balance;
            
            afterBalance.ShouldBe(beforeBalance + amount - fee);
            afterVoteBalance.ShouldBe(beforeVoteBalance - amount);

            return withdraw;
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

        #region Token Method

        //token action
        public TransactionResultDto TransferToken(string from, string to, long amount, string symbol = "ELF")
        {
            TokenService.SetAccount(from);

            return TokenService.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
            {
                Symbol = symbol,
                Amount = amount,
                To = to.ConvertAddress(),
                Memo = $"{amount}."
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
                Issuer = issuer.ConvertAddress(),
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
                To = toAddress.ConvertAddress()
            });

            return issue;
        }

        public TransactionResultDto ApproveToken(string from, string to, long amount, string symbol = "ELF")
        {
            TokenService.SetAccount(from);

            var approve = TokenService.ExecuteMethodWithResult(TokenMethod.Approve, new ApproveInput
            {
                Symbol = symbol,
                Spender = to.ConvertAddress(),
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
                Spender = to.ConvertAddress(),
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
                From =from.ConvertAddress(),
                To = to.ConvertAddress(),
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