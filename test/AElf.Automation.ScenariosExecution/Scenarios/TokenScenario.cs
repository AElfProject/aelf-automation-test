using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.WebApi.Dto;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken.Messages;
using AElf.Types;
using Nito.AsyncEx;

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
                        Owner = Address.Parse(from),
                        Spender = Address.Parse(to),
                        Symbol = "ELF"
                    }).Allowance;
                
                var token = Token.GetNewTester(from);
                if (allowance - amount < 0)
                {
                    
                    var txResult1 = token.ExecuteMethodWithResult(TokenMethod.Approve, new ApproveInput
                    {
                        Amount = 1000,
                        Spender = Address.Parse(to),
                        Symbol = "ELF"
                    });
                    if (txResult1.InfoMsg is TransactionResultDto txDto1)
                    {
                        if (txDto1.Status == "Mined")
                            Logger.WriteInfo($"Approve success - from {from} to {to} with amount {amount}.");
                        else
                            return;
                    }
                }
                
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
            }
        }

        public void PrepareAccountBalance()
        {
            //prepare bp account token
            CollectAllBpTokensToBp0();
            Logger.WriteInfo($"BEGIN: bp1 token balance: {Token.GetUserBalance(BpNodes.First().Account)}");
            
            var publicKeys = Election.CallViewMethod<PublicKeysList>(ElectionMethod.GetCandidates, new Empty());
            var isAnnounced = publicKeys.Value.Select(o => o.ToByteArray().ToHex())
                .Contains(FullNodes.First().PublicKey);
            var tokenBalance = Token.GetUserBalance(FullNodes.First().Account);

            var bp = BpNodes.First();
            var token = Token.GetNewTester(bp.Account, bp.Password);
            
            //prepare full node token
            Logger.WriteInfo("Prepare token for all full nodes.");
            if (!isAnnounced && tokenBalance == 0)
            {
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
            Logger.WriteInfo("Prepare token for all testers.");
            foreach(var user in AllTesters)
            {
                var balance = Token.GetUserBalance(user);
                if (balance >= 500_000) continue;
                token.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                {
                    Symbol = "ELF",
                    Amount = 500_000 - balance,
                    To = Address.Parse(user),
                    Memo = $"Transfer for testing - {Guid.NewGuid()}"
                });
                Thread.Sleep(10);
            }

            token.CheckTransactionResultList();
            Logger.WriteInfo($"END: bp1 token balance: {Token.GetUserBalance(BpNodes.First().Account)}");
        }

        private void CollectAllBpTokensToBp0()
        {
            Logger.WriteInfo("Transfer all bps token to first bp for testing.");
            var bp0 = BpNodes.First();
            foreach (var bp in BpNodes.Skip(1))
            {
                var balance = Token.GetUserBalance(bp.Account);
                if(balance<1000)
                    continue;
                
                //transfer
                Token.SetAccount(bp.Account, bp.Password);
                Token.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                {
                    Amount = balance,
                    Symbol = "ELF",
                    To = Address.Parse(bp0.Account),
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