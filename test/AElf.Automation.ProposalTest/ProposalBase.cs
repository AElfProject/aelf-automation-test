using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.SDK.Models;
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
        private string AccountDir { get; } = CommonHelper.GetCurrentDataDir();
        protected static List<string> Tester { get; set; }
        protected string NativeToken;
        protected static int MinersCount { get; set; }
        protected List<string> Miners { get; set; }
        protected static ContractServices Services { get; set; }

        protected ProposalBase()
        {
            _config = ConfigHelper.Config;
            var testEnvironment = ConfigHelper.Config.TestEnvironment;
            _environmentInfo =
                ConfigHelper.Config.EnvironmentInfos.Find(o => o.Environment.Contains(testEnvironment));
        }

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
            NativeToken = GetNativeToken();
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

            var testUsers = accounts.FindAll(o => !Services.ConsensusService.GetCurrentMinersPubkey().Contains(o));
            if (testUsers.Count >= _config.UserCount) return testUsers.Take(_config.UserCount).ToList();

            var newAccounts = GenerateTestUsers(nodeManager, _config.UserCount - testUsers.Count);
            testUsers.AddRange(newAccounts);
            return testUsers;
        }

        private ContractServices GetContractServices()
        {
            InitAccount = _environmentInfo.InitAccount;
            var url = _environmentInfo.Url;
            var password = _environmentInfo.Password;

            Services = new ContractServices(url, InitAccount, AccountDir, password);
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

        private string GetNativeToken()
        {
            var token = Services.TokenService.GetPrimaryTokenSymbol();
            return token;
        }

        protected void GetMiners()
        {
            Miners = new List<string>();
            var miners =
                Services.ConsensusService.CallViewMethod<MinerList>(ConsensusMethod.GetCurrentMinerList, new Empty());
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
                Services.TokenService.ExecuteMethodWithResult(TokenMethod.Create, createTransactionInput);
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
                Services.TokenService.ExecuteMethodWithResult(TokenMethod.Issue, issueInput);
            if (issueResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Failed)
                throw new Exception($"Issue token {Symbol} Failed");
        }

        protected void TransferToTester()
        {
            GetMiners();
            foreach (var tester in Tester)
            {
                var balance = Services.TokenService.GetUserBalance(tester);
                if (balance >= 100_00000000) continue;
                Services.TokenService.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
                {
                    Symbol = NativeToken,
                    To = AddressHelper.Base58StringToAddress(tester),
                    Amount = 1000_00000000,
                    Memo = "Transfer to tester"
                });

                balance = Services.TokenService.GetUserBalance(tester);
                Logger.Info($"Tester {tester} {NativeToken} balance is {balance}");
            }

            foreach (var miner in Miners)
            {
                var balance = Services.TokenService.GetUserBalance(miner);
                if (balance >= 100_00000000) continue;
                Services.TokenService.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
                {
                    Symbol = NativeToken,
                    To = AddressHelper.Base58StringToAddress(miner),
                    Amount = 1000_00000000,
                    Memo = "Transfer to miners"
                });

                balance = Services.TokenService.GetUserBalance(miner);
                Logger.Info($"Miner {miner} {NativeToken} balance is {balance}");
            }
        }
    }
}