using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using Google.Protobuf;
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
            ExecuteStandaloneTask(new Action[]
            {
                TransferAction,
                ApproveTransferAction,
                ParallelTransferAction,
                ParallelTransferFromAction,
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
                    Memo = $"T-{Guid.NewGuid()}"
                }, out var existed);
                if (existed) return;
                transferTxResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);

                var transferFee = transferTxResult.GetDefaultTransactionFee();
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

        private void ParallelTransferAction()
        {
            var list = new List<TxItem>();
            //generate transactions
            for (var i = 1; i < 6; i++)
            {
                GetTransferPair(out var from, out var to, out var amount);
                var rawTx = Services.NodeManager.GenerateRawTransaction(from, Token.ContractAddress,
                    nameof(TokenMethod.Transfer),
                    new TransferInput
                    {
                        To = to.ConvertAddress(),
                        Amount = amount,
                        Symbol = "ELF",
                        Memo = $"PT-{Guid.NewGuid()}"
                    });
                var transaction =
                    Transaction.Parser.ParseFrom(ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx)));
                var txId = transaction.GetHash().ToHex();
                list.Add(new TxItem
                {
                    From = from,
                    To = to,
                    Amount = amount,
                    RawTx = rawTx,
                    TxId = txId
                });
            }

            var testers = list.Select(o => o.From).Concat(list.Select(o => o.To)).Distinct().ToList();

            var beforeBalances = new Dictionary<string, long>();
            foreach (var tester in testers)
            {
                var balance = Token.GetUserBalance(tester);
                beforeBalances.Add(tester, balance);
            }

            //execute transactions
            foreach (var rawTx in list.Select(o => o.RawTx).ToList()) Services.NodeManager.SendTransaction(rawTx);

            Services.NodeManager.CheckTransactionListResult(list.Select(o => o.TxId).ToList());

            //verify result
            var afterBalances = new Dictionary<string, long>();
            foreach (var tester in testers)
            {
                var balance = Token.GetUserBalance(tester);
                afterBalances.Add(tester, balance);
            }

            //check fee
            var checkResult = true;
            foreach (var tx in list)
            {
                var transactionResult =
                    AsyncHelper.RunSync(() => Services.NodeManager.ApiClient.GetTransactionResultAsync(tx.TxId));
                var status = transactionResult.Status.ConvertTransactionResultStatus();
                if (status == TransactionResultStatus.Mined)
                {
                    beforeBalances[tx.From] -= tx.Amount + transactionResult.GetDefaultTransactionFee();
                    beforeBalances[tx.To] += tx.Amount;
                }
                else if (status == TransactionResultStatus.Failed) // failed tx only check tx fee
                {
                    beforeBalances[tx.From] -= transactionResult.GetDefaultTransactionFee();
                }
                else
                {
                    checkResult = false;
                    Logger.Error($"Transaction status {status} cannot check result.");
                }
            }

            if (checkResult)
                //check all balance
                foreach (var tester in testers)
                    beforeBalances[tester].ShouldBe(afterBalances[tester]);
        }

        private void ParallelTransferFromAction()
        {
            var approveList = new List<TxItem>();
            //execute approve operation
            for (var i = 1; i < 6; i++)
            {
                GetTransferPair(out var from, out var to, out var amount);
                var txId = Services.NodeManager.SendTransaction(from, Token.ContractAddress,
                    nameof(TokenMethod.Approve),
                    new ApproveInput
                    {
                        Spender = to.ConvertAddress(),
                        Symbol = "ELF",
                        Amount = amount
                    });
                approveList.Add(new TxItem
                {
                    From = from,
                    To = to,
                    Amount = amount,
                    TxId = txId
                });
            }

            Services.NodeManager.CheckTransactionListResult(approveList.Select(o => o.TxId).ToList());

            //execute transferFrom operation
            var transferFromList = new List<TxItem>();
            foreach (var tx in approveList)
            {
                var rawTx = Services.NodeManager.GenerateRawTransaction(tx.To, Token.ContractAddress,
                    nameof(TokenMethod.TransferFrom),
                    new TransferFromInput
                    {
                        From = tx.From.ConvertAddress(),
                        To = tx.To.ConvertAddress(),
                        Amount = tx.Amount,
                        Symbol = "ELF",
                        Memo = $"TF-{Guid.NewGuid()}"
                    });
                var transaction =
                    Transaction.Parser.ParseFrom(ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(rawTx)));
                var txId = transaction.GetHash().ToHex();
                transferFromList.Add(new TxItem
                {
                    From = tx.To,
                    To = tx.From,
                    Amount = tx.Amount,
                    RawTx = rawTx,
                    TxId = txId
                });
            }

            var testers = transferFromList.Select(o => o.From).Concat(transferFromList.Select(o => o.To)).Distinct()
                .ToList();

            var beforeBalances = new Dictionary<string, long>();
            foreach (var tester in testers)
            {
                var balance = Token.GetUserBalance(tester);
                beforeBalances.Add(tester, balance);
            }

            //execute transactions
            foreach (var rawTx in transferFromList.Select(o => o.RawTx).ToList())
                Services.NodeManager.SendTransaction(rawTx);
            Services.NodeManager.CheckTransactionListResult(transferFromList.Select(o => o.TxId).ToList());

            //verify result
            var afterBalances = new Dictionary<string, long>();
            foreach (var tester in testers)
            {
                var balance = Token.GetUserBalance(tester);
                afterBalances.Add(tester, balance);
            }

            //check fee
            var checkResult = true;
            foreach (var tx in transferFromList)
            {
                var transactionResult =
                    AsyncHelper.RunSync(() => Services.NodeManager.ApiClient.GetTransactionResultAsync(tx.TxId));
                var status = transactionResult.Status.ConvertTransactionResultStatus();
                if (status == TransactionResultStatus.Mined)
                {
                    beforeBalances[tx.From] += tx.Amount - transactionResult.GetDefaultTransactionFee();
                    beforeBalances[tx.To] -= tx.Amount;
                }
                else if (status == TransactionResultStatus.Failed) // failed tx only check tx fee
                {
                    beforeBalances[tx.From] -= transactionResult.GetDefaultTransactionFee();
                }
                else
                {
                    checkResult = false;
                    Logger.Error($"Transaction status {status} cannot check result.");
                }
            }

            if (checkResult)
                //check all balance
                foreach (var tester in testers)
                    beforeBalances[tester].ShouldBe(afterBalances[tester]);
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
                            Logger.Info($"Approve success - from {from} to {to} with amount {amount}.");
                        else
                            Logger.Error(
                                $"Allowance check failed. {NodeOption.NativeTokenSymbol}: {allowance + 1000_00000000}/{newAllowance}");

                        allowance = newAllowance;
                    }
                    else
                    {
                        return;
                    }
                }

                var beforeFrom = Token.GetUserBalance(from);
                var beforeTo = Token.GetUserBalance(to);
                token = Token.GetNewTester(to);
                var transactionResult = token.ExecuteMethodWithResult(TokenMethod.TransferFrom, new TransferFromInput
                {
                    Amount = amount,
                    From = from.ConvertAddress(),
                    To = to.ConvertAddress(),
                    Symbol = NodeOption.NativeTokenSymbol,
                    Memo = $"TF-{Guid.NewGuid()}"
                }, out var existed);
                if (existed) return; //check tx whether existed
                if (transactionResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined) return;

                var transactionFee = transactionResult.GetDefaultTransactionFee();
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
                    Memo = $"{Guid.NewGuid()}"
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

    public struct TxItem
    {
        public string TxId { get; set; }
        public string RawTx { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public long Amount { get; set; }
    }
}