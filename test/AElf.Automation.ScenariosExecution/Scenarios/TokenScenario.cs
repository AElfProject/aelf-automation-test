using System;
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf.WellKnownTypes;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.WebApi.Dto;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken.Messages;
using AElf.Types;

namespace AElf.Automation.ScenariosExecution.Scenarios
{
    public class TokenScenario : BaseScenario
    {
        public TokenContract Token { get; set; }
        public ElectionContract Election { get; set; }

        public List<string> Testers { get; }

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

        public void TransferAction()
        {
            GetTransferPair(out var from, out var to, out var amount);
            try
            {
                //Token.SetAccount(from);
                var token = Token.GetNewTester(from);
                token.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
                {
                    Amount = amount,
                    Symbol = "ELF",
                    To = Address.Parse(to),
                    Memo = $"Transfer amount={amount} with Guid={Guid.NewGuid()}"
                });
                Logger.WriteInfo($"Transfer success - from {from} to {to} with amount {amount}.");
            }
            catch (Exception e)
            {
                Logger.WriteError($"TransferAction: {e.Message}");
                throw;
            }
        }

        public void ApproveTransferAction()
        {
            GetTransferPair(out var from, out var to, out var amount);
            try
            {
                //Token.SetAccount(from);
                var token = Token.GetNewTester(from);
                var txResult1 = token.ExecuteMethodWithResult(TokenMethod.Approve, new ApproveInput
                {
                    Amount = amount,
                    Spender = Address.Parse(to),
                    Symbol = "ELF"
                });
                if (txResult1.InfoMsg is TransactionResultDto txDto1)
                {
                    if (txDto1.Status == "Mined")
                        Logger.WriteInfo($"Approve success - from {from} to {to} with amount {amount}.");
                }

                var approveResult = Token.CallViewMethod<GetAllowanceOutput>(TokenMethod.GetAllowance,
                    new GetAllowanceInput
                    {
                        Owner = Address.Parse(from),
                        Spender = Address.Parse(to),
                        Symbol = "ELF"
                    }).Allowance;
                if (approveResult - amount < 0)
                    return;

                //Token.SetAccount(to);
                token = Token.GetNewTester(to);
                var txResult2 = token.ExecuteMethodWithResult(TokenMethod.TransferFrom, new TransferFromInput
                {
                    Amount = amount,
                    From = Address.Parse(from),
                    To = Address.Parse(to),
                    Symbol = "ELF",
                    Memo = $"TransferFrom amount={amount} with Guid={Guid.NewGuid()}"
                });
                if (!(txResult2.InfoMsg is TransactionResultDto txDto2)) return;
                if (txDto2.Status == "Mined")
                    Logger.WriteInfo($"TransferFrom success - from {@from} to {to} with amount {amount}.");
            }
            catch (Exception e)
            {
                Logger.WriteError($"ApproveTransferAction: {e.Message}");
                throw;
            }
        }

        public void PrepareAccountBalance()
        {
            //prepare bp account token
            var publicKeys = Election.CallViewMethod<PublicKeysList>(ElectionMethod.GetCandidates, new Empty());
            var isAnnounced = publicKeys.Value.Select(o => o.ToByteArray().ToHex())
                .Contains(FullNodes.First().PublicKey);
            var tokenBalance = Token.GetUserBalance(FullNodes.First().Account);

            if (!isAnnounced && tokenBalance == 0)
            {
                var bp = BpNodes.First();
                //Token.SetAccount(bp.Account, bp.Password);
                var token = Token.GetNewTester(bp.Account, bp.Password);
                foreach (var fullAccount in FullNodes.Select(o => o.Account))
                {
                    token.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                    {
                        Symbol = "ELF",
                        Amount = 200_000,
                        To = Address.Parse(fullAccount),
                        Memo = "Transfer for announcement event"
                    });
                }

                token.CheckTransactionResultList();
            }

            //prepare other user token
            var otherBp = BpNodes.Last();
            //Token.SetAccount(otherBp.Account, otherBp.Password);
            var token1 = Token.GetNewTester(otherBp.Account, otherBp.Password);
            Logger.WriteInfo($"Last bp token balance is : {Token.GetUserBalance(otherBp.Account)}");
            foreach (var user in AllTesters)
            {
                var balance = Token.GetUserBalance(user);
                if (balance < 500_000)
                {
                    token1.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
                    {
                        Symbol = "ELF",
                        Amount = 500_000 - balance,
                        To = Address.Parse(user),
                        Memo = $"Transfer for testing - {Guid.NewGuid()}"
                    });
                }
            }

            token1.CheckTransactionResultList();
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