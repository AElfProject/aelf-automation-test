using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common.Utils;
using AElfChain.SDK.Models;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Shouldly;
using Volo.Abp.Threading;

namespace AElf.Automation.ScenariosExecution.Scenarios
{
    public class TokenScenario : BaseScenario
    {
        public new static readonly ILog Logger = Log4NetHelper.GetLogger();

        public TokenScenario()
        {
            InitializeScenario();

            Token = Services.TokenService;
            Election = Services.ElectionService;
            Testers = AllTesters.GetRange(50, 30);
            PrintTesters(nameof(TokenScenario), Testers);
        }

        public TokenContract Token { get; set; }
        public ElectionContract Election { get; set; }
        public List<string> Testers { get; }

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
            ExecuteStandaloneTask(actions: new Action[]
            {
                TransferAction,
                ApproveTransferAction,
                () => PrepareTesterToken(Testers),
                UpdateEndpointAction
            });
        }

        private void TransferAction()
        {
            GetTransferPair(out var from, out var to, out var amount);
            try
            {
                var token = Token.GetNewTester(from);
                var beforeFrom = Token.GetUserBalance(from);
                var beforeTo = Token.GetUserBalance(to);
                var transferTxResult = token.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
                {
                    Amount = amount,
                    Symbol = NodeOption.NativeTokenSymbol,
                    To = AddressHelper.Base58StringToAddress(to),
                    Memo = $"Transfer amount={amount} with Guid={Guid.NewGuid()}"
                });
                transferTxResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                var transferFee = transferTxResult.TransactionFee.GetDefaultTransactionFee();
                var afterFrom = Token.GetUserBalance(from);
                var afterTo = Token.GetUserBalance(to);
                var result = true;

                //check balance process
                if (beforeFrom != afterFrom + transferFee + amount)
                {
                    Logger.Error(
                        $"Transfer balance check failed. From owner {NodeOption.NativeTokenSymbol}: {beforeFrom}/{afterFrom + transferFee + amount}");
                    result = false;
                }

                if (beforeTo != afterTo - amount)
                {
                    Logger.Error(
                        $"Transfer balance check failed. To owner {NodeOption.NativeTokenSymbol}: {beforeTo}/{afterTo - amount}");
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
                var allowance = Token.GetAllowance(from, to);
                var token = Token.GetNewTester(from);
                if (allowance - amount < 0)
                {
                    //add approve
                    var txResult1 = token.ExecuteMethodWithResult(TokenMethod.Approve, new ApproveInput
                    {
                        Amount = 1000_00000000,
                        Spender = AddressHelper.Base58StringToAddress(to),
                        Symbol = NodeOption.NativeTokenSymbol
                    });
                    //check allowance
                    if (txResult1.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Mined)
                    {
                        var newAllowance = Token.GetAllowance(from, to);
                        if (newAllowance == allowance + 1000_00000000)
                        {
                            Logger.Info($"Approve success - from {from} to {to} with amount {amount}.");
                        }
                        else
                        {
                            Logger.Error(
                                $"Allowance check failed. {NodeOption.NativeTokenSymbol}: {allowance + 1000_00000000}/{newAllowance}");
                        }

                        allowance = newAllowance;
                    }
                    else
                        return;
                }

                var beforeFrom = Token.GetUserBalance(from);
                var beforeTo = Token.GetUserBalance(to);
                var tokenStub = Token.GetTestStub<TokenContractContainer.TokenContractStub>(to);
                var transactionResult = AsyncHelper.RunSync(() => tokenStub.TransferFrom.SendAsync(new TransferFromInput
                {
                    Amount = amount,
                    From = from.ConvertAddress(),
                    To = to.ConvertAddress(),
                    Symbol = NodeOption.NativeTokenSymbol,
                    Memo = $"TransferFrom amount={amount} with Guid={Guid.NewGuid()}"
                }));
                if (transactionResult.TransactionResult.Status != TransactionResultStatus.Mined) return;

                var transactionFee = transactionResult.TransactionResult.TransactionFee.GetDefaultTransactionFee();
                var afterFrom = Token.GetUserBalance(from);
                var afterTo = Token.GetUserBalance(to);
                var afterAllowance = Token.GetAllowance(from, to);
                var checkResult = true;

                //check TransferFrom result process
                if (beforeFrom - amount != afterFrom)
                {
                    Logger.Error($"TransferFrom from balance check failed: {beforeFrom - amount}/{afterFrom}");
                    checkResult = false;
                }

                if (beforeTo - transactionFee + amount != afterTo)
                {
                    Logger.Error(
                        $"TransferFrom to balance check failed: {beforeTo - transactionFee + amount}/{afterTo}.");
                    checkResult = false;
                }

                if (afterAllowance != allowance - amount)
                {
                    Logger.Error($"TransferFrom allowance check failed: {afterAllowance}/{allowance - amount}");
                    checkResult = false;
                }

                if (checkResult)
                    Logger.Info(
                        $"TransferFrom {from}->{to} with amount {amount} success.");
            }
            catch (Exception e)
            {
                Logger.Error($"ApproveTransferAction: {e}");
            }
        }

        public void PrepareAccountBalance()
        {
            //prepare bp account token
            CollectPartBpTokensToBp0();
            Logger.Info($"BEGIN: bp1 token balance: {Token.GetUserBalance(AllNodes.First().Account)}");

            var publicKeysList = Election.CallViewMethod<PubkeyList>(ElectionMethod.GetCandidates, new Empty());
            var candidatePublicKeys = publicKeysList.Value.Select(o => o.ToByteArray().ToHex()).ToList();

            var bp = AllNodes.First();
            var token = Token.GetNewTester(bp.Account, bp.Password);

            //prepare full node token
            Logger.Info("Prepare token for all full nodes.");
            foreach (var fullNode in AllNodes)
            {
                if (candidatePublicKeys.Contains(fullNode.PublicKey)) continue;

                var tokenBalance = Token.GetUserBalance(fullNode.Account);
                if (tokenBalance > 100_000_00000000) continue;

                token.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                {
                    Symbol = NodeOption.NativeTokenSymbol,
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
                    Symbol = NodeOption.NativeTokenSymbol,
                    Amount = 500_000_00000000 - balance,
                    To = AddressHelper.Base58StringToAddress(user),
                    Memo = $"Transfer for testing - {Guid.NewGuid()}"
                });
                Thread.Sleep(10);
            }

            token.CheckTransactionResultList();
            Logger.Info($"END: bp1 token balance: {Token.GetUserBalance(AllNodes.First().Account)}");
        }

        private void GetTransferPair(out string accountFrom, out string accountTo, out long amount)
        {
            while (true)
            {
                var randomNo = GenerateRandomNumber(0, Testers.Count - 1);
                var acc = Testers[randomNo];
                var balance = Token.GetUserBalance(acc);
                if (balance < 100_00000000)
                    continue;

                accountFrom = acc;
                accountTo = randomNo == 0 ? Testers.Last() : Testers[randomNo - 1];
                amount = (randomNo % 10 + 1) * 10000;

                return;
            }
        }
    }
}