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
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.ProposalTest
{
    public class ProposalBase
    {
        private readonly EnvironmentInfo _environmentInfo;
        private static ConfigInfo _config;
        private string AccountDir { get; } = CommonHelper.GetCurrentDataDir();

        protected static readonly ILog Logger = Log4NetHelper.GetLogger();
        protected static List<string> Tester { get; set; }
        protected static readonly string NativeToken = NodeOption.NativeTokenSymbol;
        protected static string InitAccount;
        protected static string Symbol;

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
            {
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
            }

            if (sleepSeconds != 0)
                Thread.Sleep(1000 * sleepSeconds);
        }

        protected void Initialize()
        {
            if (Services == null)
                Services = GetContractServices();
            Tester = GenerateOrGetTestUsers();
            if (Symbol == null)
                ProposalPrepare();
            TransferToTester();
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

            var testUsers = accounts.FindAll(o => !Services.ConsensusService.GetCurrentMiners().Contains(o));
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
                Decimals = 2,
                IsBurnable = true,
                Issuer = Services.CallAccount,
                TokenName = "Token of test",
                TotalSupply = 5_0000_0000
            };
            var result =
                Services.TokenService.ExecuteMethodWithResult(TokenMethod.Create, createTransactionInput);
            if (result.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Failed)
                Assert.IsTrue(false, $"Create token {Symbol} Failed");

            Logger.Info($"Create token {Symbol} success");

            Logger.Info($"Issue {Symbol} token");
            var issueInput = new IssueInput
            {
                Symbol = Symbol,
                To = Services.CallAccount,
                Amount = 400000000
            };
            var issueResult =
                Services.TokenService.ExecuteMethodWithResult(TokenMethod.Issue, issueInput);
            if (issueResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Failed)
                Assert.IsTrue(false, $"Issue token {Symbol} Failed");
        }

        private void TransferToTester()
        {
            GetMiners();
            foreach (var tester in Tester)
            {
                var balance = Services.TokenService.GetUserBalance(tester);
                while (balance == 0)
                {
                    Services.TokenService.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
                    {
                        Symbol = NativeToken,
                        To = AddressHelper.Base58StringToAddress(tester),
                        Amount = 1000,
                        Memo = "Transfer to organization address"
                    });

                    balance = Services.TokenService.GetUserBalance(tester);
                }
            }
            
            foreach (var miner in Miners)
            {
                var balance = Services.TokenService.GetUserBalance(miner);
                while (balance == 0)
                {
                    Services.TokenService.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
                    {
                        Symbol = NativeToken,
                        To = AddressHelper.Base58StringToAddress(miner),
                        Amount = 1000,
                        Memo = "Transfer to organization address"
                    });

                    balance = Services.TokenService.GetUserBalance(miner);
                }
            }
        }
    }
}