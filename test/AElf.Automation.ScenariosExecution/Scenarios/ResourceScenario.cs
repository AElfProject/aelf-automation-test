using System;
using System.Collections.Generic;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenConverter;
using AElf.Types;
using AElfChain.SDK.Models;
using log4net;

namespace AElf.Automation.ScenariosExecution.Scenarios
{
    public class ResourceScenario : BaseScenario
    {
        public new static readonly ILog Logger = Log4NetHelper.GetLogger();

        public ResourceScenario()
        {
            InitializeScenario();

            Token = Services.TokenService;
            TokenConverter = Services.TokenConverterService;
            Testers = AllTesters.GetRange(20, 30);
            PrintTesters(nameof(ResourceScenario), Testers);

            SetAllowanceForResourceTest();
        }

        public TokenContract Token { get; set; }
        public TokenConverterContract TokenConverter { get; set; }
        public List<string> Testers { get; }

        public void RunResourceScenario()
        {
            ExecuteContinuousTasks(new Action[]
            {
                BuyResourceAction,
                SellResourceAction
            }, true, 2);
        }

        public void RunResourceScenarioJob()
        {
            ExecuteStandaloneTask(new Action[]
            {
                BuyResourceAction,
                SellResourceAction,
                () => PrepareTesterToken(Testers),
                UpdateEndpointAction
            });
        }

        private void BuyResourceAction()
        {
            const int testTimes = 3;
            var resSymbol = GetRandomResSymbol();
            var tokenUsers = GetAvailableTokenUser(NodeOption.NativeTokenSymbol, testTimes);
            foreach (var user in tokenUsers)
            {
                //before
                var elfBeforeBalance = Token.GetUserBalance(user);
                var resBeforeBalance = Token.GetUserBalance(user, resSymbol);
                var amount = (long) GenerateRandomNumber(100, 200) * 100000000;
                var tokenConverter = TokenConverter.GetNewTester(user);
                var buyResult = tokenConverter.ExecuteMethodWithResult(TokenConverterMethod.Buy, new BuyInput
                {
                    Amount = amount,
                    Symbol = resSymbol
                }, out var existed);
                if (existed) continue;
                if (buyResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Mined)
                {
                    var transactionFee = buyResult.TransactionFee.GetDefaultTransactionFee();
                    var elfAfterBalance = Token.GetUserBalance(user);
                    var resAfterBalance = Token.GetUserBalance(user, resSymbol);
                    if (resBeforeBalance + amount == resAfterBalance)
                    {
                        var cost = elfBeforeBalance + transactionFee - elfAfterBalance;
                        Logger.Info(
                            $"Buy resource {resSymbol}={amount} success. Price(ELF/{resSymbol}): {(double)cost / (double)amount:0.0000}");
                    }
                    else
                    {
                        Logger.Error(
                            $"Buy resource - verify failed. {resSymbol}: {resBeforeBalance + amount}/{resAfterBalance}");
                    }
                }
            }
        }

        private void SellResourceAction()
        {
            const int testTimes = 2;
            var resSymbol = GetRandomResSymbol();
            var resourceUsers = GetAvailableTokenUser(resSymbol, testTimes);
            foreach (var user in resourceUsers)
            {
                //before
                var elfBeforeBalance = Token.GetUserBalance(user);
                var resBeforeBalance = Token.GetUserBalance(user, resSymbol);
                var amount = (long) GenerateRandomNumber(100, 200) * 100000000;
                var tokenConverter = TokenConverter.GetNewTester(user);
                var sellResult = tokenConverter.ExecuteMethodWithResult(TokenConverterMethod.Sell, new SellInput
                {
                    Amount = amount,
                    Symbol = resSymbol
                }, out var existed);
                if (existed) continue;
                if (sellResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Mined)
                {
                    var transactionFee = sellResult.TransactionFee.GetDefaultTransactionFee();
                    var elfAfterBalance = Token.GetUserBalance(user);
                    var resAfterBalance = Token.GetUserBalance(user, resSymbol);
                    if (resAfterBalance == resBeforeBalance - amount)
                    {
                        var got = elfAfterBalance + transactionFee - elfBeforeBalance;
                        Logger.Info(
                            $"Sell resource {resSymbol}={amount} success. Price(ELF/{resSymbol}): {(double)got / (double)amount:0.0000}");
                    }
                    else
                    {
                        Logger.Error(
                            $"Sell resource verify failed. {resSymbol}: {resBeforeBalance + amount}/{resAfterBalance}");
                    }
                }
            }
        }

        private IEnumerable<string> GetAvailableTokenUser(string symbol, int number)
        {
            var users = new List<string>();
            var count = 0;
            foreach (var user in Testers.GetRange(1, Testers.Count - 1))
            {
                var balance = Token.GetUserBalance(user, symbol);
                if (balance < 200_00000000)
                    continue;
                users.Add(user);
                count++;
                if (count == number)
                    break;
            }

            return users;
        }

        private void SetAllowanceForResourceTest()
        {
            foreach (var user in Testers.GetRange(1, Testers.Count - 1))
            {
                Token.SetAccount(user);
                Token.ExecuteMethodWithTxId(TokenMethod.Approve, new ApproveInput
                {
                    Spender = AddressHelper.Base58StringToAddress(TokenConverter.ContractAddress),
                    Symbol = NodeOption.NativeTokenSymbol,
                    Amount = 100_000_00000000
                });
                Token.ExecuteMethodWithTxId(TokenMethod.Approve, new ApproveInput
                {
                    Spender = AddressHelper.Base58StringToAddress(TokenConverter.ContractAddress),
                    Symbol = "RAM",
                    Amount = 100_000_00000000
                });
                Token.ExecuteMethodWithTxId(TokenMethod.Approve, new ApproveInput
                {
                    Spender = AddressHelper.Base58StringToAddress(TokenConverter.ContractAddress),
                    Symbol = "CPU",
                    Amount = 100_000_00000000
                });
                Token.ExecuteMethodWithTxId(TokenMethod.Approve, new ApproveInput
                {
                    Spender = AddressHelper.Base58StringToAddress(TokenConverter.ContractAddress),
                    Symbol = "NET",
                    Amount = 100_000_00000000
                });
                Token.ExecuteMethodWithTxId(TokenMethod.Approve, new ApproveInput
                {
                    Spender = AddressHelper.Base58StringToAddress(TokenConverter.ContractAddress),
                    Symbol = "STO",
                    Amount = 100_000_00000000
                });
            }

            Token.CheckTransactionResultList();
        }

        private string GetRandomResSymbol()
        {
            var symbols = new[] {"CPU", "RAM", "NET", "STO"};
            var id = GenerateRandomNumber(0, symbols.Length - 1);

            return symbols[id];
        }
    }
}