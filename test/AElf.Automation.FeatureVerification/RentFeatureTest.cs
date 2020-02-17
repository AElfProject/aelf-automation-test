using System.Linq;
using System.Threading.Tasks;
using Acs3;
using AElf.Contracts.Association;
using AElf.Contracts.Configuration;
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
    public class RentFeatureTest
    {
        public ILogHelper Logger = LogHelper.GetLogger();
        public INodeManager MainNode { get; set; }
        public INodeManager SideNode { get; set; }
        
        public GenesisContract Genesis { get; set; }

        public RentFeatureTest()
        {
            Log4NetHelper.LogInit();
            Logger.InitLogHelper();
            MainNode = new NodeManager("192.168.197.40:8000");

            NodeInfoHelper.SetConfig("nodes-env2-side1");
            var bpNode = NodeInfoHelper.Config.Nodes.First();
            SideNode = new NodeManager(bpNode.Endpoint);
            Genesis = SideNode.GetGenesisContract(bpNode.Account);
        }

        [TestMethod]
        public void UpdateRentalTest()
        {
            var authority = new AuthorityManager(SideNode);
            var bps = authority.GetCurrentMiners();
            var token = Genesis.GetTokenContract();
            foreach (var bp in bps)
            {
                if (bp == bps.First()) continue;
                token.IssueBalance(bps.First(), bp, 10000_00000000, token.GetPrimaryTokenSymbol());
            }

            var crossChain = Genesis.GetCrossChainStub();
            var association = Genesis.GetAssociationAuthContract();
            var defaultParliament = authority.GetGenesisOwnerAddress();
            var organizationInput = new CreateOrganizationInput
            {
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MaximalAbstentionThreshold = 0,
                    MaximalRejectionThreshold = 0,
                    MinimalApprovalThreshold = 2,
                    MinimalVoteThreshold = 2
                },
                OrganizationMemberList = new OrganizationMemberList
                {
                    OrganizationMembers = {defaultParliament, bps.First().ConvertAddress()}
                },
                ProposerWhiteList = new ProposerWhiteList
                {
                    Proposers = {defaultParliament, bps.First().ConvertAddress()}
                }
            };
            var associationAddress = association.CreateOrganization(organizationInput);
            var updateRentalInput = new UpdateRentalInput
            {
                Rental =
                {
                    {"CPU", 1000},
                    {"RAM", 1000},
                    {"DISK", 10},
                    {"NET", 1000}
                }
            };
            var proposal = association.CreateProposal(token.ContractAddress, nameof(TokenMethod.UpdateRental),
                updateRentalInput, associationAddress, bps.First());
            association.ApproveProposal(proposal, bps.First());
            var approveProposal = authority.ExecuteTransactionWithAuthority(association.ContractAddress,
                nameof(AssociationMethod.Approve), proposal, defaultParliament, bps, bps.First());
            approveProposal.Status.ShouldBe(TransactionResultStatus.Mined);
            var release = association.ReleaseProposal(proposal, bps.First());
            release.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        [TestMethod]
        public async Task UpdateRentedResources()
        {
            var token = Genesis.GetTokenStub();
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
            var token = Genesis.GetTokenStub();
            var resourceUsage = await token.GetResourceUsage.CallAsync(new Empty());
            Logger.Info(JsonConvert.SerializeObject(resourceUsage));
        }

        [TestMethod]
        public async Task QueryOwningRental()
        {
            var token = Genesis.GetTokenStub();
            var rental = await token.GetOwningRental.CallAsync(new Empty());
            foreach (var item in rental.ResourceAmount)
            {
                Logger.Info($"{item.Key}, {item.Value}");
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
            {
                await tokenConverter.Buy.SendAsync(new BuyInput
                {
                    Symbol = symbol,
                    Amount = 100000_00000000,
                });
            }

            Logger.Info($"Account: {bps.First()}");
            foreach (var symbol in symbols)
            {
                var balance = token.GetUserBalance(bps.First(), symbol);
                Logger.Info($"{symbol}={balance}");
            }
        }

        [TestMethod]
        public void GetTokenInfo()
        {
            var authority = new AuthorityManager(SideNode);
            var bps = authority.GetCurrentMiners();
            var genesis = SideNode.GetGenesisContract(bps.First());
            var token = genesis.GetTokenContract();
            var symbols = new[] {"CPU", "RAM", "DISK", "NET"};
            foreach (var symbol in symbols)
            {
                var result = token.GetTokenInfo(symbol);
                Logger.Info($"{symbol} issuer={result.Issuer};amount={result.TotalSupply}");
            }
        }

        [TestMethod]
        public void SideChain_GetBalance()
        {
            var authority = new AuthorityManager(SideNode);
            var bps = authority.GetCurrentMiners();
            var genesis = SideNode.GetGenesisContract(bps.First());
            var token = genesis.GetTokenContract();
            var symbols = new[] {"CPU", "RAM", "DISK", "NET"};
            foreach (var bp in bps)
            {
                Logger.Info($"Account: {bp}");
                foreach (var symbol in symbols)
                {
                    var balance = token.GetUserBalance(bp, symbol);
                    Logger.Info($"{symbol}={balance}");
                }
            }

            foreach (var symbol in symbols)
            {
                var balance = token.GetUserBalance(genesis.GetConsensusContract().ContractAddress, symbol);
                Logger.Info($"{symbol}={balance}");
            }
        }
    }
}