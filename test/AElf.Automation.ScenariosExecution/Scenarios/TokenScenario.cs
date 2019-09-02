using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.SDK.Models;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Shouldly;
using Volo.Abp.Threading;

namespace AElf.Automation.ScenariosExecution.Scenarios
{
    public class TokenScenario : BaseScenario
    {
        public TokenContract Token { get; set; }
        public TokenContractContainer.TokenContractStub TokenStub { get; set; }
        public ElectionContract Election { get; set; }
        public List<string> Testers { get; }

        public new static readonly ILog Logger = Log4NetHelper.GetLogger();

        public TokenScenario()
        {
            InitializeScenario();

            Token = Services.TokenService;
            Election = Services.ElectionService;
            Testers = AllTesters.GetRange(25, 25);
        }

        public void RunTokenScenario()
        {
            ExecuteContinuousTasks(new Action[]
            {
                TransferAction,
                ApproveTransferAction
            }, true, 2);
        }

        public void TokenScenarioJob()
        {
            ExecuteStandaloneTask(new Action[]
            {
                TransferAction,
                ApproveTransferAction
            });
        }

        private void TransferAction()
        {
            GetTransferPair(out var from, out var to, out var amount);
            try
            {
                var token = Token.GetNewTester(from);
                var beforeA = Token.GetUserBalance(from);
                var beforeB = Token.GetUserBalance(to);
                var tokenResult = token.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
                {
                    Amount = amount,
                    Symbol = "ELF",
                    To = AddressHelper.Base58StringToAddress(to),
                    Memo = $"Transfer amount={amount} with Guid={Guid.NewGuid()}"
                });
                tokenResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                var afterA = Token.GetUserBalance(from);
                var afterB = Token.GetUserBalance(to);
                var result = true;
                if (beforeA != afterA + amount)
                {
                    Logger.Error(
                        $"Transfer failed, amount check not correct. From owner ELF: {beforeA}/{afterA + amount}");
                    result = false;
                }

                if (beforeB != afterB - amount)
                {
                    Logger.Error(
                        $"Transfer failed, amount check not correct. To owner ELF: {beforeB}/{afterB - amount}");
                    result = false;
                }

                if (result)
                    Logger.Info($"Transfer success - from {from} to {to} with amount {amount}.");
            }
            catch (Exception e)
            {
                Logger.Error($"TransferAction: {e.Message}");
            }
        }

        private void ApproveTransferAction()
        {
            GetTransferPair(out var from, out var to, out var amount);
            try
            {
                var allowance = Token.CallViewMethod<GetAllowanceOutput>(TokenMethod.GetAllowance,
                    new GetAllowanceInput
                    {
                        Owner = AddressHelper.Base58StringToAddress(from),
                        Spender = AddressHelper.Base58StringToAddress(to),
                        Symbol = "ELF"
                    }).Allowance;

                var token = Token.GetNewTester(from);
                if (allowance - amount < 0)
                {
                    var txResult1 = token.ExecuteMethodWithResult(TokenMethod.Approve, new ApproveInput
                    {
                        Amount = 1000_00000000,
                        Spender = AddressHelper.Base58StringToAddress(to),
                        Symbol = "ELF"
                    });
                    if (txResult1.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Mined)
                        Logger.Info($"Approve success - from {from} to {to} with amount {amount}.");
                    else
                        return;
                }

                var beforeFrom = Token.GetUserBalance(from);
                var beforeTo = Token.GetUserBalance(to);
                var tokenStub = Token.GetTestStub<TokenContractContainer.TokenContractStub>(to);
                var transactionResult = AsyncHelper.RunSync(() => tokenStub.TransferFrom.SendAsync(new TransferFromInput
                {
                    Amount = amount,
                    From = AddressHelper.Base58StringToAddress(from),
                    To = AddressHelper.Base58StringToAddress(to),
                    Symbol = "ELF",
                    Memo = $"TransferFrom amount={amount} with Guid={Guid.NewGuid()}"
                }));
                if (transactionResult.TransactionResult.Status != TransactionResultStatus.Mined) return;
                var afterFrom = Token.GetUserBalance(from);
                var afterTo = Token.GetUserBalance(to);
                if (beforeFrom - amount == afterFrom && beforeTo + amount == afterTo)
                    Logger.Info($"TransferFrom success - from {from} to {to} with amount {amount}.");
                else
                    Logger.Error(
                        $"TransferFrom amount {amount} got some balance issue. From: {beforeFrom}/{afterFrom}, To:{beforeTo}/{afterTo}");
            }
            catch (Exception e)
            {
                Logger.Error($"ApproveTransferAction: {e.Message}");
            }
        }

        public void PrepareAccountBalance()
        {
            //prepare bp account token
            CollectAllBpTokensToBp0();
            Logger.Info($"BEGIN: bp1 token balance: {Token.GetUserBalance(BpNodes.First().Account)}");

            var publicKeysList = Election.CallViewMethod<PubkeyList>(ElectionMethod.GetCandidates, new Empty());
            var candidatePublicKeys = publicKeysList.Value.Select(o => o.ToByteArray().ToHex()).ToList();

            var bp = BpNodes.First();
            var token = Token.GetNewTester(bp.Account, bp.Password);

            //prepare full node token
            Logger.Info("Prepare token for all full nodes.");
            foreach (var fullNode in FullNodes)
            {
                if (candidatePublicKeys.Contains(fullNode.PublicKey)) continue;

                var tokenBalance = Token.GetUserBalance(fullNode.Account);
                if (tokenBalance > 100_000_00000000) continue;

                token.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                {
                    Symbol = "ELF",
                    Amount = 200_000_00000000,
                    To = AddressHelper.Base58StringToAddress(fullNode.Account),
                    Memo = "Transfer for announcement event"
                });
            }

            token.CheckTransactionResultList();

            //prepare other user token
            Logger.Info("Prepare token for all testers.");
            foreach (var user in AllTesters)
            {
                var balance = Token.GetUserBalance(user);
                if (balance >= 500_000_00000000) continue;

                token.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                {
                    Symbol = "ELF",
                    Amount = 500_000_00000000 - balance,
                    To = AddressHelper.Base58StringToAddress(user),
                    Memo = $"Transfer for testing - {Guid.NewGuid()}"
                });
                Thread.Sleep(10);
            }

            token.CheckTransactionResultList();
            Logger.Info($"END: bp1 token balance: {Token.GetUserBalance(BpNodes.First().Account)}");
        }

        private void CollectAllBpTokensToBp0()
        {
            Logger.Info("Transfer all bps token to first bp for testing.");
            var bp0 = BpNodes.First();
            foreach (var bp in BpNodes.Skip(1))
            {
                var balance = Token.GetUserBalance(bp.Account);
                if (balance < 1000_00000000)
                    continue;

                //transfer
                Token.SetAccount(bp.Account, bp.Password);
                Token.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                {
                    Amount = balance,
                    Symbol = "ELF",
                    To = AddressHelper.Base58StringToAddress(bp0.Account),
                    Memo = "Collect all token from other bps."
                });
            }

            Token.CheckTransactionResultList();
        }

        private void GetTransferPair(out string accountFrom, out string accountTo, out long amount)
        {
            while (true)
            {
                var randomNo = GenerateRandomNumber(0, Testers.Count - 1);
                var acc = Testers[randomNo];
                var balance = Token.GetUserBalance(acc);
                if (balance < 100)
                    continue;

                accountFrom = acc;
                accountTo = randomNo == 0 ? Testers.Last() : Testers[randomNo - 1];
                amount = (long) randomNo % 10 + 1;
                return;
            }
        }
    }
}