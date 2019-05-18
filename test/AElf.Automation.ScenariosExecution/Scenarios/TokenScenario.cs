using System;
using System.Linq;
using Google.Protobuf.WellKnownTypes;
using AElf.Automation.Common.Contracts;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken.Messages;

namespace AElf.Automation.ScenariosExecution.Scenarios
{
    public class TokenScenario : BaseScenario
    {
        public TokenContract Token { get; set; }
        public ElectionContract Election { get; set; }

        public TokenScenario()
        {
            InitializeScenario();

            Token = Services.TokenService;
            Election = Services.ElectionService;
        }

        public void TransferAction()
        {
            GetTransferPair(out var from, out var to, out var amount);
            try
            {
                Token.SetAccount(from);
                Token.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                {
                    Amount = amount,
                    Symbol = "ELF",
                    To = Address.Parse(to),
                    Memo = $"Transfer amount={amount} with Guid={Guid.NewGuid()}"
                });
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
                Token.SetAccount(from);
                Token.ExecuteMethodWithTxId(TokenMethod.Approve, new ApproveInput
                {
                    Amount = amount,
                    Spender = Address.Parse(to),
                    Symbol = "ELF"
                });

                Token.SetAccount(to);
                Token.ExecuteMethodWithTxId(TokenMethod.TransferFrom, new TransferFromInput
                {
                    Amount = amount,
                    From = Address.Parse(from),
                    To = Address.Parse(to),
                    Symbol = "ELF",
                    Memo = $"TransferFrom amount={amount} with Guid={Guid.NewGuid()}"
                });
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
                Token.SetAccount(bp.Account, bp.Password);
                foreach (var fullAccount in FullNodes.Select(o => o.Account))
                {
                    Token.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                    {
                        Symbol = "ELF",
                        Amount = 100_000,
                        To = Address.Parse(fullAccount),
                        Memo = "Transfer for announcement."
                    });
                }
            }

            //prepare other user token
            var otherBp = BpNodes.Last();
            Token.SetAccount(otherBp.Account, otherBp.Password);
            foreach (var user in TestUsers)
            {
                var balance = Token.GetUserBalance(user);
                if (balance < 10_000)
                {
                    Token.ExecuteMethodWithTxId(TokenMethod.Transfer, new TransferInput
                    {
                        Symbol = "ELF",
                        Amount = 10_000 - balance,
                        To = Address.Parse(user),
                        Memo = "Transfer for announcement."
                    });
                }
            }

            Token.CheckTransactionResultList();
        }

        private void GetTransferPair(out string accountFrom, out string accountTo, out long amount)
        {
            while (true)
            {
                var randomNo = GenerateRandomNumber(TestUsers.Count - 1);
                var acc = TestUsers[randomNo];
                var balance = Token.GetUserBalance(acc);
                if (balance < 100)
                    continue;

                accountFrom = acc;
                accountTo = TestUsers[randomNo - 1];
                amount = randomNo % 10 + 1;
                return;
            }
        }

        private static int GenerateRandomNumber(int maxValue)
        {
            var rd = new Random(DateTime.Now.Millisecond);
            return rd.Next(1, maxValue);
        }
    }
}