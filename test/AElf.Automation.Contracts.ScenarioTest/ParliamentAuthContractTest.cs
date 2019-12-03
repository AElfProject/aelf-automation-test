using System;
using System.Collections.Generic;
using System.Threading;
using Acs3;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Contracts.ParliamentAuth;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElfChain.SDK;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;
using ApproveInput = Acs3.ApproveInput;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class ParliamentAuthContractTest
    {
        private static readonly ILog _logger = Log4NetHelper.GetLogger();
        public ParliamentAuthContract NewParliament;
        public ParliamentAuthContract Parliament;
        public string Symbol = NodeOption.NativeTokenSymbol;
        protected ContractTester Tester;
        public INodeManager NodeManager { get; set; }
        public IApiService ApiService { get; set; }
        public List<string> UserList { get; set; }
        protected static int MinersCount { get; set; }
        protected List<string> Miners { get; set; }

        public string InitAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        public string TestAccount { get; } = "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK";
        public string Full { get; } = "2V2UjHQGH8WT4TWnzebxnzo9uVboo67ZFbLjzJNTLrervAxnws";

        private static string RpcUrl { get; } = "http://192.168.197.40:8000";

        [TestInitialize]
        public void Initialize()
        {
            #region Basic Preparation

            //Init Logger
            Log4NetHelper.LogInit("ParliamentTest_");

            #endregion

            NodeManager = new NodeManager(RpcUrl);
            ApiService = NodeManager.ApiService;
            var contractServices = new ContractServices(NodeManager, InitAccount, "Main");
            Tester = new ContractTester(contractServices);
            Parliament = Tester.ParliamentService;
//            DeployAndInitialize();
            GetMiners();
        }

        [TestMethod]
        public void CreateOrganization()
        {
            var result = NewParliament.ExecuteMethodWithResult(ParliamentMethod.CreateOrganization,
                new CreateOrganizationInput
                {
                    ReleaseThreshold = 10000
                });
            var organizationAddress = result.ReadableReturnValue.Replace("\"", "");
            _logger.Info($"organization address is : {organizationAddress}");

            var organization =
                NewParliament.CallViewMethod<Organization>(ParliamentMethod.GetOrganization,
                    AddressHelper.Base58StringToAddress(organizationAddress));

            Tester.TokenService.SetAccount(InitAccount);
            var transfer = Tester.TokenService.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
            {
                Symbol = Symbol,
                Amount = 1000,
                Memo = "transfer to Organization",
                To = AddressHelper.Base58StringToAddress(organizationAddress)
            });
        }

        // new Parliament ZuTnjdqwK8vNcyypzn34YXfCeM1c6yDTGfrKvJuwmWqnSePSm
        // organization 4TbVeRPki6dQQWVoWHRibXKSPsLjYYgKc6sFnmUonFGewujEm

        [TestMethod]
        [DataRow("4TbVeRPki6dQQWVoWHRibXKSPsLjYYgKc6sFnmUonFGewujEm",
            "ZuTnjdqwK8vNcyypzn34YXfCeM1c6yDTGfrKvJuwmWqnSePSm")]
        public void CreateProposal(string organizationAddress, string contractAddress)
        {
            NewParliament = new ParliamentAuthContract(NodeManager, InitAccount, contractAddress);
            var transferInput = new TransferInput
            {
                Symbol = Symbol,
                Amount = 100,
                To = AddressHelper.Base58StringToAddress(TestAccount),
                Memo = "Transfer"
            };
            var createProposalInput = new CreateProposalInput
            {
                ContractMethodName = nameof(TokenMethod.Transfer),
                ToAddress = AddressHelper.Base58StringToAddress(Tester.TokenService.ContractAddress),
                Params = transferInput.ToByteString(),
                ExpiredTime = DateTime.UtcNow.AddDays(1).ToTimestamp(),
                OrganizationAddress = AddressHelper.Base58StringToAddress(organizationAddress)
            };

            NewParliament.SetAccount(TestAccount);
            var result =
                NewParliament.ExecuteMethodWithResult(ParliamentMethod.CreateProposal,
                    createProposalInput);
            var proposal = result.ReadableReturnValue;
            _logger.Info($"Proposal is : {proposal}");
        }

        [TestMethod]
        [DataRow("d00bea66e43d59df5a73c272379fd91a2a6f918b2a3375629d5f39184dba5422",
            "ZuTnjdqwK8vNcyypzn34YXfCeM1c6yDTGfrKvJuwmWqnSePSm")]
        public void GetProposal(string proposalId, string contractAddress)
        {
            NewParliament = new ParliamentAuthContract(NodeManager, InitAccount, contractAddress);
            var result =
                NewParliament.CallViewMethod<ProposalOutput>(ParliamentMethod.GetProposal,
                    HashHelper.HexStringToHash(proposalId));
            var toBeRelease = result.ToBeReleased;
            var proposalParams = result.Params.ToStringUtf8();
            var time = result.ExpiredTime;
            var organizationAddress = result.OrganizationAddress;
            var contractMethodName = result.ContractMethodName;
            var toAddress = result.ToAddress;

            _logger.Info($"proposal is {toBeRelease}");
            _logger.Info($"proposal expired time is {time} ");
            _logger.Info($"proposal params is {proposalParams} ");
            _logger.Info($"proposal organization is {organizationAddress}");
            _logger.Info($"proposal method name is {contractMethodName}");
            _logger.Info($"proposal to address is {toAddress}");
        }

        [TestMethod]
        [DataRow("d00bea66e43d59df5a73c272379fd91a2a6f918b2a3375629d5f39184dba5422",
            "ZuTnjdqwK8vNcyypzn34YXfCeM1c6yDTGfrKvJuwmWqnSePSm")]
        public void Approve(string proposalId, string contractAddress)
        {
            NewParliament = new ParliamentAuthContract(NodeManager, InitAccount, contractAddress);

            foreach (var miner in Miners)
            {
                var balance = Tester.TokenService.GetUserBalance(miner, Symbol);
                _logger.Info($"{miner} balance is {balance}");
                if (balance <= 0)
                {
                    Tester.TokenService.SetAccount(InitAccount);
                    Tester.TokenService.TransferBalance(InitAccount, miner, 1000_0000000, Symbol);
                }

                NewParliament.SetAccount(miner);
                var result =
                    NewParliament.ExecuteMethodWithResult(ParliamentMethod.Approve, new ApproveInput
                    {
                        ProposalId = HashHelper.HexStringToHash(proposalId)
                    });
                _logger.Info($"Approve is {result.ReadableReturnValue}");
            }
        }

        [TestMethod]
        [DataRow("daa3215f3832e61b4360caebd976c97419644bae4af647645b0b8e33033fca5b",
            "F5d3S7YJhSLvcBWtGw6nJ6Rx64MBgK4RpdMt6EAEDcns36qYs")]
        public void ApproveWithFullNode(string proposalId, string contractAddress)
        {
            NewParliament = new ParliamentAuthContract(NodeManager, InitAccount, contractAddress);
            var balance = Tester.TokenService.GetUserBalance(Full, Symbol);
            if (balance <= 0)
            {
                Tester.TokenService.SetAccount(InitAccount);
                Tester.TokenService.TransferBalance(InitAccount, Full, 1000_0000000, Symbol);
            }

            NewParliament.SetAccount(Full);
            var result =
                NewParliament.ExecuteMethodWithResult(ParliamentMethod.Approve, new ApproveInput
                {
                    ProposalId = HashHelper.HexStringToHash(proposalId)
                });
            _logger.Info($"Approve is {result.ReadableReturnValue}");
        }

        [TestMethod]
        [DataRow("d00bea66e43d59df5a73c272379fd91a2a6f918b2a3375629d5f39184dba5422",
            "ZuTnjdqwK8vNcyypzn34YXfCeM1c6yDTGfrKvJuwmWqnSePSm")]
        public void Release(string proposalId, string contractAddress)
        {
            NewParliament = new ParliamentAuthContract(NodeManager, InitAccount, contractAddress);
            NewParliament.SetAccount(TestAccount);
            var result =
                NewParliament.ExecuteMethodWithResult(ParliamentMethod.Release, HashHelper.HexStringToHash(proposalId));
            result.Status.ShouldBe("MINED");
        }

        private void DeployAndInitialize()
        {
            var authority = new AuthorityManager(NodeManager, InitAccount);
            var contractAddress =
                authority.DeployContractWithAuthority(InitAccount, "AElf.Contracts.ParliamentAuth.dll");

            Thread.Sleep(2000);

            _logger.Info($"{contractAddress.GetFormatted()}");
            NewParliament = new ParliamentAuthContract(NodeManager, InitAccount, contractAddress.GetFormatted());
        }

        protected void GetMiners()
        {
            Miners = new List<string>();
            var miners =
                Tester.ConsensusService.CallViewMethod<MinerList>(ConsensusMethod.GetCurrentMinerList, new Empty());
            foreach (var minersPubkey in miners.Pubkeys)
            {
                var miner = Address.FromPublicKey(minersPubkey.ToByteArray());
                Miners.Add(miner.GetFormatted());
            }

            MinersCount = Miners.Count;
        }
    }
}