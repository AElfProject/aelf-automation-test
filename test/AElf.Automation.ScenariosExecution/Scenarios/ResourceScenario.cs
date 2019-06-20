using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi.Dto;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TokenConverter;
using AElf.Types;

namespace AElf.Automation.ScenariosExecution.Scenarios
{
    public class ResourceScenario : BaseScenario
    {
        public TokenContract Token { get; set; }

        public FeeReceiverContract FeeReceiver { get; set; }
        public TokenConverterContract TokenConverter { get; set; }
        public List<string> Testers { get; }

        public ResourceScenario()
        {
            InitializeScenario();

            Token = Services.TokenService;
            FeeReceiver = ContractServices.FeeReceiverService;
            Testers = AllTesters.GetRange(5, 20);

            InitializeTokenConverter();
        }

        public void RunResourceScenario()
        {
            ExecuteContinuousTasks(new Action[]
            {
                BuyResourceAction,
                SellResourceAction
            }, true, 2);
        }

        public void ResourceScenarioJob()
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
                if (!(buyResult.InfoMsg is TransactionResultDto txDto)) continue;
                if (txDto.Status == "Mined")
                    Logger.WriteInfo(
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
                    Symbol = connector.Symbol,
                });
                if (!(sellResult.InfoMsg is TransactionResultDto txDto)) continue;
                if (txDto.Status == "Mined")
                    Logger.WriteInfo(
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
            TokenConverter = new TokenConverterContract(Services.ApiHelper, Services.CallAddress);

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
                    TotalSupply = 100_0000
                });
                if (!(createResult.InfoMsg is TransactionResultDto createDto)) continue;
                if (createDto.Status == "Mined")
                    Logger.WriteInfo($"Create resource {connector.Symbol} successful.");

                var issueResult = token.ExecuteMethodWithResult(TokenMethod.Issue, new IssueInput
                {
                    Symbol = connector.Symbol,
                    Amount = 100_0000,
                    Memo = $"Issue {connector.Symbol} token",
                    To = Address.Parse(TokenConverter.ContractAddress)
                });
                if (!(issueResult.InfoMsg is TransactionResultDto issueDto)) continue;
                if (issueDto.Status == "Mined")
                    Logger.WriteInfo($"Issue total amount 100_0000 resource {connector.Symbol} successful.");
            }

            //initialize resources
            TokenConverter.ExecuteMethodWithResult(TokenConverterMethod.Initialize, new InitializeInput
            {
                BaseTokenSymbol = "ELF",
                FeeRate = "0.01",
                ManagerAddress = Address.Parse(Testers[0]),
                TokenContractAddress = Address.Parse(Token.ContractAddress),
                FeeReceiverAddress = Address.Parse(FeeReceiver.ContractAddress),
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
                    Spender = Address.Parse(TokenConverter.ContractAddress),
                    Symbol = "ELF",
                    Amount = 1000_0000
                });
                Token.ExecuteMethodWithTxId(TokenMethod.Approve, new ApproveInput
                {
                    Spender = Address.Parse(TokenConverter.ContractAddress),
                    Symbol = RamConnector.Symbol,
                    Amount = 1000_0000
                });
                Token.ExecuteMethodWithTxId(TokenMethod.Approve, new ApproveInput
                {
                    Spender = Address.Parse(TokenConverter.ContractAddress),
                    Symbol = CpuConnector.Symbol,
                    Amount = 1000_0000
                });
                Token.ExecuteMethodWithTxId(TokenMethod.Approve, new ApproveInput
                {
                    Spender = Address.Parse(TokenConverter.ContractAddress),
                    Symbol = NetConnector.Symbol,
                    Amount = 1000_0000
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

        private Connector ElfConnector = new Connector
        {
            Symbol = "ELF",
            VirtualBalance = 1000_0000,
            Weight = "0.5",
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = true
        };

        private Connector RamConnector = new Connector
        {
            Symbol = $"RAM{CommonHelper.RandomString(4, false)}",
            VirtualBalance = 0,
            Weight = "0.5",
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = false
        };

        private Connector CpuConnector = new Connector
        {
            Symbol = $"CPU{CommonHelper.RandomString(4, false)}",
            VirtualBalance = 0,
            Weight = "0.5",
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = false
        };

        private Connector NetConnector = new Connector
        {
            Symbol = $"NET{CommonHelper.RandomString(4, false)}",
            VirtualBalance = 0,
            Weight = "0.5",
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = false
        };

        private IEnumerable<Connector> Connectors => new List<Connector>
        {
            RamConnector,
            CpuConnector,
            NetConnector
        };

        private Connector GetRandomConnector()
        {
            var id = GenerateRandomNumber(0, Connectors.Count() - 1);

            return Connectors.ToArray()[id];
        }
    }
}