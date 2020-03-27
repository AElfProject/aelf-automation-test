using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using log4net;

namespace AElf.Automation.ProposalTest
{
    public class ProposalBase
    {
        private static ConfigInfo _config;

        protected static readonly ILog Logger = Log4NetHelper.GetLogger();
        protected static string InitAccount;
        protected static string Symbol;
        private readonly EnvironmentInfo _environmentInfo;
        protected string TokenSymbol;

        protected ProposalBase()
        {
            _config = ConfigHelper.Config;
            var testEnvironment = ConfigHelper.Config.TestEnvironment;
            _environmentInfo =
                ConfigHelper.Config.EnvironmentInfos.Find(o => o.Environment.Contains(testEnvironment));
        }

        private string AccountDir { get; } = CommonHelper.GetCurrentDataDir();
        protected static List<string> AssociationTester { get; set; }
        protected static List<string> Tester { get; set; }
        protected static int MinersCount { get; set; }
        protected List<string> Miners { get; set; }
        protected static ContractManager Services { get; set; }

        protected void ExecuteStandaloneTask(IEnumerable<Action> actions, int sleepSeconds = 0,
            bool interrupted = false)
        {
            foreach (var action in actions)
                try
                {
                    action.Invoke();
                }
                catch (Exception e)
                {
                    Logger.Error($"Execute action {action.Method.Name} got exception: {e.Message}", e);
                    if (interrupted)
                        break;
                }

            if (sleepSeconds != 0)
                Thread.Sleep(1000 * sleepSeconds);
        }

        protected void Initialize()
        {
            if (Services == null)
                Services = GetContractServices();
            Tester = GenerateOrGetTestUsers();
            AssociationTester = GenerateOrGetTestUsers(Tester);
            TokenSymbol = Services.Token.GetPrimaryTokenSymbol();
            if (Symbol == null)
                ProposalPrepare();
        }

        protected static int GenerateRandomNumber(int min, int max)
        {
            var random = new Random(Guid.NewGuid().GetHashCode());
            return random.Next(min, max + 1);
        }

        private List<string> GenerateOrGetTestUsers()
        {
            var url = _environmentInfo.Url;
            var nodeManager = new NodeManager(url, AccountDir);

            var accounts = nodeManager.ListAccounts();
            GetMiners();
            var testUsers = accounts.FindAll(o => !Miners.Contains(o));
            if (testUsers.Count >= _config.UserCount) return testUsers.Take(_config.UserCount).ToList();

            var newAccounts = GenerateTestUsers(nodeManager, _config.UserCount - testUsers.Count);
            testUsers.AddRange(newAccounts);
            return testUsers;
        }

        private List<string> GenerateOrGetTestUsers(ICollection<string> testers)
        {
            var url = _environmentInfo.Url;
            var nodeManager = new NodeManager(url, AccountDir);

            var accounts = nodeManager.ListAccounts();

            var testUsers = accounts.FindAll(o => !Miners.Contains(o) && !testers.Contains(o));
            if (testUsers.Count >= _config.UserCount) return testUsers.Take(_config.UserCount).ToList();

            var newAccounts = GenerateTestUsers(nodeManager, _config.UserCount - testUsers.Count);
            testUsers.AddRange(newAccounts);
            return testUsers;
        }

        private ContractManager GetContractServices()
        {
            InitAccount = _environmentInfo.InitAccount;
            var url = _environmentInfo.Url;
            var password = _environmentInfo.Password;

            Services = new ContractManager(url, InitAccount);
            return Services;
        }

        private List<string> GenerateTestUsers(INodeManager manager, int count)
        {
            var accounts = new List<string>();
            Parallel.For(0, count, i =>
            {
                var account = manager.NewAccount();
                accounts.Add(account);
            });

            return accounts;
        }

        protected void GetMiners()
        {
            Miners = new List<string>();
            var miners =
                Services.Consensus.CallViewMethod<MinerList>(ConsensusMethod.GetCurrentMinerList, new Empty());
            foreach (var minersPubkey in miners.Pubkeys)
            {
                var miner = Address.FromPublicKey(minersPubkey.ToByteArray());
                Miners.Add(miner.GetFormatted());
            }

            MinersCount = Miners.Count;
        }

        private void ProposalPrepare()
        {
            Logger.Info("Create token for test: ");

            Symbol = $"TEST{CommonHelper.RandomString(4, false)}";
            var createTransactionInput = new CreateInput
            {
                Symbol = Symbol,
                Decimals = 8,
                IsBurnable = true,
                Issuer = Services.CallAccount,
                TokenName = "Token of test",
                TotalSupply = 10_0000_0000_00000000
            };
            var result =
                Services.Token.ExecuteMethodWithResult(TokenMethod.Create, createTransactionInput);
            if (result.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Failed)
                throw new Exception($"Create token {Symbol} Failed");

            Logger.Info($"Create token {Symbol} success");

            Logger.Info($"Issue {Symbol} token");
            var issueInput = new IssueInput
            {
                Symbol = Symbol,
                To = Services.CallAccount,
                Amount = 10_0000_0000_00000000
            };
            var issueResult =
                Services.Token.ExecuteMethodWithResult(TokenMethod.Issue, issueInput);
            if (issueResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Failed)
                throw new Exception($"Issue token {Symbol} Failed");
        }

        protected void TransferToTester()
        {
            GetMiners();
            foreach (var tester in Tester)
            {
                var balance = Services.Token.GetUserBalance(tester, TokenSymbol);
                if (balance >= 1000_00000000) continue;
                Services.Token.TransferBalance(InitAccount, tester, 1000_00000000, TokenSymbol);
                balance = Services.Token.GetUserBalance(tester);
                Logger.Info($"Tester {tester} {TokenSymbol} balance is {balance}");
            }

            foreach (var tester in AssociationTester)
            {
                var balance = Services.Token.GetUserBalance(tester, TokenSymbol);
                if (balance >= 1000_00000000) continue;
                Services.Token.TransferBalance(InitAccount, tester, 1000_00000000, TokenSymbol);
                balance = Services.Token.GetUserBalance(tester);
                Logger.Info($"Tester {tester} {TokenSymbol} balance is {balance}");
            }

            foreach (var miner in Miners)
            {
                var balance = Services.Token.GetUserBalance(miner, TokenSymbol);
                if (balance >= 1000_00000000) continue;
                Services.Token.TransferBalance(InitAccount, miner, 1000_00000000, TokenSymbol);

                balance = Services.Token.GetUserBalance(miner);
                Logger.Info($"Miner {miner} {TokenSymbol} balance is {balance}");
            }
        }
    }
}