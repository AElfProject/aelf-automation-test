using System;
using System.Collections.Generic;
using System.Linq;
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

        private readonly Connector CpuConnector = new Connector
        {
            Symbol = $"CPU{CommonHelper.RandomString(4, false)}",
            VirtualBalance = 0,
            Weight = "0.5",
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = false
        };

        private readonly Connector ElfConnector = new Connector
        {
            Symbol = NodeOption.NativeTokenSymbol,
            VirtualBalance = 100_000_000,
            Weight = "0.5",
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = true
        };

        private readonly Connector NetConnector = new Connector
        {
            Symbol = $"NET{CommonHelper.RandomString(4, false)}",
            VirtualBalance = 0,
            Weight = "0.5",
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = false
        };

        private readonly Connector RamConnector = new Connector
        {
            Symbol = $"RAM{CommonHelper.RandomString(4, false)}",
            VirtualBalance = 0,
            Weight = "0.5",
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = false
        };

        public ResourceScenario()
        {
            InitializeScenario();

            Token = Services.TokenService;
            Treasury = Services.TreasuryService;
            Testers = AllTesters.GetRange(5, 20);

            InitializeTokenConverter();
        }

        public TokenContract Token { get; set; }

        public TreasuryContract Treasury { get; set; }
        public TokenConverterContract TokenConverter { get; set; }
        public List<string> Testers { get; }

        private IEnumerable<Connector> Connectors => new List<Connector>
        {
            RamConnector,
            CpuConnector,
            NetConnector
        };

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
                SellResourceAction
            });
        }

        private void BuyResourceAction()
        {
            const int testTimes = 3;
            var connector = GetRandomConnector();
            var tokenUsers = GetAvailableBuyUser(testTimes);
            foreach (var user in tokenUsers)
            {
                var amount = GenerateRandomNumber(100, 200);
                var tokenConverter = TokenConverter.GetNewTester(user);
                var buyResult = tokenConverter.ExecuteMethodWithResult(TokenConverterMethod.Buy, new BuyInput
                {
                    Amount = amount,
                    Symbol = connector.Symbol
                });
                if (buyResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Mined)
                    Logger.Info(
                        $"Buy resource - {user} buy resource {connector.Symbol} cost token {amount}");
            }
        }

        private void SellResourceAction()
        {
            const int testTimes = 2;
            var connector = GetRandomConnector();
            var resourceUsers = GetAvailableSellUser(connector.Symbol, testTimes);
            foreach (var user in resourceUsers)
            {
                var amount = GenerateRandomNumber(100, 200);
                var tokenConverter = TokenConverter.GetNewTester(user);
                var sellResult = tokenConverter.ExecuteMethodWithResult(TokenConverterMethod.Sell, new SellInput
                {
                    Amount = amount,
                    Symbol = connector.Symbol
                });
                if (sellResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Mined)
                    Logger.Info(
                        $"Sell resource - {user} sell resource {connector.Symbol} with amount {amount}");
            }
        }

        private IEnumerable<string> GetAvailableBuyUser(int number)
        {
            var users = new List<string>();
            var count = 0;
            foreach (var user in Testers.GetRange(1, Testers.Count - 1))
            {
                var balance = Token.GetUserBalance(user);
                if (balance < 1000)
                    continue;
                users.Add(user);
                count++;
                if (count == number)
                    break;
            }

            return users;
        }

        private IEnumerable<string> GetAvailableSellUser(string symbol, int number)
        {
            var users = new List<string>();
            var count = 0;
            foreach (var user in Testers.GetRange(1, Testers.Count - 1))
            {
                var balance = Token.GetUserBalance(user, symbol);
                if (balance < 500)
                    continue;
                users.Add(user);
                count++;
                if (count == number)
                    break;
            }

            return users;
        }

        private void InitializeTokenConverter()
        {
            TokenConverter = new TokenConverterContract(Services.NodeManager, Services.CallAddress);

            //Create and issue all resources token
            var token = Token.GetNewTester(Testers[0]);
            foreach (var connector in Connectors)
            {
                while (true)
                {
                    var tokenInfo = GetTokenInfo(connector.Symbol);
                    if (tokenInfo.TotalSupply != 0)
                    {
                        connector.Symbol =
                            $"{connector.Symbol.Replace(connector.Symbol.Substring(3), CommonHelper.RandomString(4, false))}";
                        continue;
                    }

                    break;
                }

                var createResult = token.ExecuteMethodWithResult(TokenMethod.Create, new CreateInput
                {
                    Symbol = connector.Symbol,
                    Decimals = 2,
                    IsBurnable = true,
                    Issuer = token.CallAccount,
                    TokenName = $"{connector.Symbol} Resource",
                    TotalSupply = 100_000_000
                });
                if (createResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Mined)
                    Logger.Info($"Create resource {connector.Symbol} successful.");

                var issueResult = token.ExecuteMethodWithResult(TokenMethod.Issue, new IssueInput
                {
                    Symbol = connector.Symbol,
                    Amount = 100_000_000,
                    Memo = $"Issue {connector.Symbol} token",
                    To = AddressHelper.Base58StringToAddress(TokenConverter.ContractAddress)
                });
                if (issueResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Mined)
                    Logger.Info($"Issue total amount 100_0000 resource {connector.Symbol} successful.");
            }

            //initialize resources
            TokenConverter.ExecuteMethodWithResult(TokenConverterMethod.Initialize, new InitializeInput
            {
                BaseTokenSymbol = NodeOption.NativeTokenSymbol,
                FeeRate = "0.01",
                ManagerAddress = AddressHelper.Base58StringToAddress(Testers[0]),
                TokenContractAddress = AddressHelper.Base58StringToAddress(Token.ContractAddress),
                FeeReceiverAddress = AddressHelper.Base58StringToAddress(Treasury.ContractAddress),
                Connectors = {ElfConnector, RamConnector, CpuConnector, NetConnector}
            });

            //set allowance for test
            SetAllowanceForResourceTest();
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
                    Amount = 100_000_000
                });
                Token.ExecuteMethodWithTxId(TokenMethod.Approve, new ApproveInput
                {
                    Spender = AddressHelper.Base58StringToAddress(TokenConverter.ContractAddress),
                    Symbol = RamConnector.Symbol,
                    Amount = 100_000_000
                });
                Token.ExecuteMethodWithTxId(TokenMethod.Approve, new ApproveInput
                {
                    Spender = AddressHelper.Base58StringToAddress(TokenConverter.ContractAddress),
                    Symbol = CpuConnector.Symbol,
                    Amount = 100_000_000
                });
                Token.ExecuteMethodWithTxId(TokenMethod.Approve, new ApproveInput
                {
                    Spender = AddressHelper.Base58StringToAddress(TokenConverter.ContractAddress),
                    Symbol = NetConnector.Symbol,
                    Amount = 100_000_000
                });
            }

            Token.CheckTransactionResultList();
        }

        private TokenInfo GetTokenInfo(string symbol)
        {
            var tokenInfo = Token.CallViewMethod<TokenInfo>(TokenMethod.GetTokenInfo, new GetTokenInfoInput
            {
                Symbol = symbol
            });
            return tokenInfo;
        }

        private Connector GetRandomConnector()
        {
            var id = GenerateRandomNumber(0, Connectors.Count() - 1);

            return Connectors.ToArray()[id];
        }
    }
}