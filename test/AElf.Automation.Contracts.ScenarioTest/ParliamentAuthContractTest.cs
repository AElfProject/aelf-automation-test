using System;
using System.Collections.Generic;
using System.Threading;
using Acs3;
using AElf.Automation.Common;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Contracts.ParliamentAuth;
using AElf.Types;
using AElfChain.SDK;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class ParliamentAuthContractTest
    {
        private static readonly ILog _logger = Log4NetHelper.GetLogger();
        protected ContractTester Tester;
        public INodeManager NodeManager { get; set; }
        public IApiService ApiService { get; set; }
        public List<string> UserList { get; set; }
        public string Symbol = NodeOption.NativeTokenSymbol;
        public ParliamentAuthContract Parliament;
        public ParliamentAuthContract NewParliament;
        protected static int MinersCount { get; set; }
        protected List<string> Miners { get; set; }

        public string InitAccount { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";
        public string TestAccount { get; } = "2RCLmZQ2291xDwSbDEJR6nLhFJcMkyfrVTq1i1YxWC4SdY49a6";
        public string Full { get; } = "28qLVdGMokanMAp9GwfEqiWnzzNifh8LS9as6mzJFX1gQBB823";

        private static string RpcUrl { get; } = "http://192.168.197.56:8001";

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
                    ReleaseThreshold = 10000,
                    ProposerAuthorityRequired = false
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

        // new Parliament 2F5C128Srw5rHCXoSY2C7uT5sAku48mkgiaTTp1Hiprhbb7ED9
        // organization 236ZzzR3otu8jxhCgdxSuRWSoKid8rzbKPx3Bws86rM3PC1qEZ

        [TestMethod]
        [DataRow("236ZzzR3otu8jxhCgdxSuRWSoKid8rzbKPx3Bws86rM3PC1qEZ",
            "2F5C128Srw5rHCXoSY2C7uT5sAku48mkgiaTTp1Hiprhbb7ED9")]
        public void CreateProposal(string organizationAddress, string contractAddress)
        {
            NewParliament = new ParliamentAuthContract(NodeManager, InitAccount, contractAddress);
            var transferInput = new TransferInput()
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
                ExpiredTime = DateTime.UtcNow.AddMinutes(60).ToTimestamp(),
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
        [DataRow("4c2dbb930af2895ad5736e87c3c4bda2a95b5590d8dde105c4ff44ca12384aef",
            "2F5C128Srw5rHCXoSY2C7uT5sAku48mkgiaTTp1Hiprhbb7ED9")]
        public void GetProposal(string proposalId, string contractAddress)
        {
            NewParliament = new ParliamentAuthContract(NodeManager, InitAccount, contractAddress);
            var result =
                NewParliament.CallViewMethod<ProposalOutput>(ParliamentMethod.GetProposal,
                    HashHelper.HexStringToHash(proposalId));
            var toBeRelease = result.ToBeReleased;
            var time = result.ExpiredTime;

            _logger.Info($"proposal is {toBeRelease}");
            _logger.Info($"proposal expired time is {time} ");
        }

        [TestMethod]
        [DataRow("4c2dbb930af2895ad5736e87c3c4bda2a95b5590d8dde105c4ff44ca12384aef",
            "2F5C128Srw5rHCXoSY2C7uT5sAku48mkgiaTTp1Hiprhbb7ED9")]
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
                    NewParliament.ExecuteMethodWithResult(ParliamentMethod.Approve, new Acs3.ApproveInput()
                    {
                        ProposalId = HashHelper.HexStringToHash(proposalId)
                    });
                _logger.Info($"Approve is {result.ReadableReturnValue}");
            }
        }
        
        [TestMethod]
        [DataRow("daa3215f3832e61b4360caebd976c97419644bae4af647645b0b8e33033fca5b",
            "2F5C128Srw5rHCXoSY2C7uT5sAku48mkgiaTTp1Hiprhbb7ED9")]
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
                    NewParliament.ExecuteMethodWithResult(ParliamentMethod.Approve, new Acs3.ApproveInput()
                    {
                        ProposalId = HashHelper.HexStringToHash(proposalId)
                    });
                _logger.Info($"Approve is {result.ReadableReturnValue}");
        }

        [TestMethod]
        [DataRow("639b015531cb04ac71544d37286abce40e57955ec5b6625ca55e1d58889bc9ce",
            "2F5C128Srw5rHCXoSY2C7uT5sAku48mkgiaTTp1Hiprhbb7ED9")]
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