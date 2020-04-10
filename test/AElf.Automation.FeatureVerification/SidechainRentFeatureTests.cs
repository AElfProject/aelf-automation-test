using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs3;
using AElf.Contracts.Association;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenConverter;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Shouldly;

namespace AElf.Automation.Contracts.ScenarioTest
{
    [TestClass]
    public class SidechainRentFeatureTests
    {
        public ILogHelper Logger = LogHelper.GetLogger();

        public SidechainRentFeatureTests()
        {
            Log4NetHelper.LogInit();
            Logger.InitLogHelper();
            MainNode = new NodeManager("192.168.197.40:8000");

            NodeInfoHelper.SetConfig("nodes-env2-side1");
            var bpNode = NodeInfoHelper.Config.Nodes.First();
            SideNode = new NodeManager(bpNode.Endpoint);
            Genesis = SideNode.GetGenesisContract(bpNode.Account);

            MainManager = new ContractManager(MainNode, bpNode.Account);
            SideManager = new ContractManager(SideNode, bpNode.Account);
        }

        public INodeManager MainNode { get; set; }
        public INodeManager SideNode { get; set; }

        public ContractManager MainManager { get; set; }
        public ContractManager SideManager { get; set; }

        public GenesisContract Genesis { get; set; }

        [TestMethod]
        public void UpdateRentalTest()
        {
            var authority = new AuthorityManager(SideNode);
            var token = Genesis.GetTokenContract();
            var organization = authority.GetGenesisOwnerAddress();
            var bps = authority.GetCurrentMiners();
            authority.ExecuteTransactionWithAuthority(token.ContractAddress, nameof(TokenMethod.UpdateRental),
                new UpdateRentalInput
                {
                    Rental =
                    {
                        {"CPU", 1000},
                        {"RAM", 500},
                        {"DISK", 4},
                        {"NET", 2}
                    }
                }, organization, bps, bps.First());
        }

        [TestMethod]
        public async Task NewOrg_UpdateRentalTest()
        {
            var proposer = SideManager.CallAddress;
            var defaultOrganization = SideManager.ParliamentAuth.GetGenesisOwnerAddress();
            var association = await CreateNewAssociationOrganization(defaultOrganization, SideManager.CallAccount);
            var updateRentalInput = new UpdateRentalInput
            {
                Rental =
                {
                    {"CPU", 1000},
                    {"RAM", 500},
                    {"DISK", 4},
                    {"NET", 2}
                }
            };
            var proposalId = SideManager.Association.CreateProposal(
                SideManager.Token.ContractAddress,
                nameof(TokenMethod.UpdateRental), updateRentalInput, association,
                proposer);
            SideManager.Association.SetAccount(proposer);
            SideManager.Association.ApproveProposal(proposalId, proposer);
            var approveProposalId = SideManager.ParliamentAuth.CreateProposal(
                SideManager.Association.ContractAddress, nameof(AssociationMethod.Approve), proposalId,
                defaultOrganization, proposer);
            var currentMiners = SideManager.Authority.GetCurrentMiners();
            foreach (var miner in currentMiners) SideManager.ParliamentAuth.ApproveProposal(approveProposalId, miner);

            SideManager.ParliamentAuth.ReleaseProposal(approveProposalId, proposer);
            SideManager.Association.ReleaseProposal(proposalId, proposer);
        }

        [TestMethod]
        public async Task QueryOwningRentalUnitValueTest()
        {
            var token = Genesis.GetTokenImplStub();
            var unitValueInfo = await token.GetOwningRentalUnitValue.CallAsync(new Empty());
            Logger.Info(JsonConvert.SerializeObject(unitValueInfo));
        }

        [TestMethod]
        public async Task UpdateRentedResources()
        {
            var token = Genesis.GetTokenImplStub();
            var transactionResult = await token.UpdateRentedResources.SendAsync(new UpdateRentedResourcesInput
            {
                ResourceAmount =
                {
                    {"CPU", 2},
                    {"RAM", 4},
                    {"DISK", 512}
                }
            });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var resourceUsage = await token.GetResourceUsage.CallAsync(new Empty());
            resourceUsage.Value["CPU"].ShouldBe(2);
            resourceUsage.Value["RAM"].ShouldBe(4);
            resourceUsage.Value["DISK"].ShouldBe(512);
        }

        [TestMethod]
        public async Task QueryResourceUsage()
        {
            var token = Genesis.GetTokenImplStub();
            var resourceUsage = await token.GetResourceUsage.CallAsync(new Empty());
            Logger.Info(JsonConvert.SerializeObject(resourceUsage));
        }

        [TestMethod]
        public async Task QueryOwningRental()
        {
            var token = Genesis.GetTokenImplStub();
            var rental = await token.GetOwningRental.CallAsync(new Empty());
            foreach (var item in rental.ResourceAmount) Logger.Info($"{item.Key}, {item.Value}");
        }

        [TestMethod]
        public async Task CheckMinerBalance()
        {
            var authority = new AuthorityManager(SideNode);
            var token = Genesis.GetTokenStub();
            var bps = authority.GetCurrentMiners();
            var symbols = new[] {"CPU", "RAM", "DISK", "NET", "STB"};
            foreach (var bp in bps)
            foreach (var symbol in symbols)
            {
                var balance = await token.GetBalance.CallAsync(new GetBalanceInput
                    {Owner = bp.ConvertAddress(), Symbol = symbol});
                Logger.Info($"{bp} {symbol}, {balance.Balance}");
            }
        }

        [TestMethod]
        public async Task MainChain_BuyResource()
        {
            var authority = new AuthorityManager(MainNode);
            var bps = authority.GetCurrentMiners();

            var genesis = MainNode.GetGenesisContract(bps.First());
            var token = genesis.GetTokenContract();
            var tokenConverter = genesis.GetTokenConverterStub();
            var symbols = new[] {"CPU", "RAM", "DISK", "NET"};
            foreach (var symbol in symbols)
                await tokenConverter.Buy.SendAsync(new BuyInput
                {
                    Symbol = symbol,
                    Amount = 10000_00000000
                });

            Logger.Info($"Account: {bps.First()}");
            foreach (var symbol in symbols)
            {
                var balance = token.GetUserBalance(bps.First(), symbol);
                Logger.Info($"{symbol}={balance}");
            }
        }

        private async Task<Address> CreateNewAssociationOrganization(Address parliamentOrgAddress, Address sideCreator)
        {
            var minimalApproveThreshold = 2;
            var minimalVoteThreshold = 2;
            var maximalAbstentionThreshold = 0;
            var maximalRejectionThreshold = 0;
            var list = new List<Address> {parliamentOrgAddress, sideCreator};
            var createOrganizationInput = new CreateOrganizationInput
            {
                OrganizationMemberList = new OrganizationMemberList
                {
                    OrganizationMembers = {list}
                },
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MinimalApprovalThreshold = minimalApproveThreshold,
                    MinimalVoteThreshold = minimalVoteThreshold,
                    MaximalAbstentionThreshold = maximalAbstentionThreshold,
                    MaximalRejectionThreshold = maximalRejectionThreshold
                },
                ProposerWhiteList = new ProposerWhiteList
                {
                    Proposers = {list}
                }
            };
            var transactionResult =
                await SideManager.AssociationStub.CreateOrganization.SendAsync(createOrganizationInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var organization = transactionResult.Output;
            Logger.Info($"Organization address: {organization.GetFormatted()}");

            return organization;
        }
    }
}