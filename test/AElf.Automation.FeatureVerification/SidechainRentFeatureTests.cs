using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs1;
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
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Shouldly;
using Volo.Abp.Threading;

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
            MainNode = new NodeManager("192.168.197.21:8000");

            NodeInfoHelper.SetConfig("nodes-env2-side2");
            var bpNode = NodeInfoHelper.Config.Nodes.First();
            SideNode = new NodeManager(bpNode.Endpoint);
            Genesis = SideNode.GetGenesisContract(bpNode.Account);

            MainManager = new ContractManager(MainNode, bpNode.Account);
            SideManager = new ContractManager(SideNode, bpNode.Account);
            AuthoritySideManager = new AuthorityManager(SideNode);
        }

        public INodeManager MainNode { get; set; }
        public INodeManager SideNode { get; set; }
        public AuthorityManager AuthoritySideManager { get; set; }
        public ContractManager MainManager { get; set; }
        public ContractManager SideManager { get; set; }

        public GenesisContract Genesis { get; set; }

        [TestMethod]
        public void UpdateRentalTest()
        {
            var input = new UpdateRentalInput
            {
                Rental =
                    {
                        {"CPU", 1000},
                        {"RAM", 500},
                        {"DISK", 4},
                        {"NET", 2}
                    }
            };
           ProposalThroughController(input,nameof(TokenMethod.UpdateRental));
        }

        [TestMethod]
        public async Task Change_UpdateRentalTest()
        {
            var proposer = SideManager.CallAddress;
            var stub = Genesis.GetTokenImplStub();
            var token = Genesis.GetTokenContract();
            var defaultOrganization = await stub.GetSideChainRentalControllerCreateInfo.CallAsync(new Empty());
            var newController = SideManager.Parliament.GetGenesisOwnerAddress();
            var input = new AuthorityInfo
            {
                OwnerAddress = newController,
                ContractAddress = SideManager.Parliament.Contract
            };
            ProposalThroughController(input,nameof(TokenContractImplContainer.TokenContractImplStub.ChangeSideChainRentalController));
            
            // use changed organization send update transaction
            var updateRentalInput = new UpdateRentalInput
            {
                Rental =
                {
                    {"CPU", 2000},
                    {"RAM", 200},
                    {"DISK", 8},
                    {"NET", 4}
                }
            };
            var update = AuthoritySideManager.ExecuteTransactionWithAuthority(token.ContractAddress,
                nameof(TokenMethod.UpdateRental), updateRentalInput, proposer, newController);
            update.Status.ShouldBe(TransactionResultStatus.Mined);
            var unitValueInfo = await stub.GetOwningRentalUnitValue.CallAsync(new Empty());
            unitValueInfo.ResourceUnitValue["CPU"].Equals(2000).ShouldBeTrue();
            
            //recover
            var recoverInput = new AuthorityInfo
            {
                OwnerAddress = defaultOrganization.OwnerAddress,
                ContractAddress = defaultOrganization.ContractAddress
            };
            var recover = AuthoritySideManager.ExecuteTransactionWithAuthority(token.ContractAddress,
                nameof(TokenContractImplContainer.TokenContractImplStub.ChangeSideChainRentalController), recoverInput, proposer, newController);
            update.Status.ShouldBe(TransactionResultStatus.Mined);
            var recoverOrganization = await stub.GetSideChainRentalControllerCreateInfo.CallAsync(new Empty());
            recoverOrganization.ShouldBe(defaultOrganization);
        }

        [TestMethod]
        public async Task QueryOwningRentalUnitValueTest()
        {
            var token = Genesis.GetTokenImplStub();
            var unitValueInfo = await token.GetOwningRentalUnitValue.CallAsync(new Empty());
            Logger.Info(JsonConvert.SerializeObject(unitValueInfo));
        }

        [TestMethod]
        public void UpdateRentedResources()
        {
            var input = new UpdateRentedResourcesInput
            {
                ResourceAmount =
                {
                    {"NET",2048},
                    {"CPU", 2},
                    {"RAM", 4},
                    {"DISK", 512}
                }
            };
            ProposalThroughController(input,nameof(TokenMethod.UpdateRentedResources));
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
            var symbols = new[] {"CPU", "RAM", "DISK", "NET", "STB","READ", "WRITE", "STORAGE", "TRAFFIC"};
            foreach (var bp in bps)
            foreach (var symbol in symbols)
            {
                var balance = await token.GetBalance.CallAsync(new GetBalanceInput
                    {Owner = bp.ConvertAddress(), Symbol = symbol});
                Logger.Info($"{bp} {symbol}, {balance.Balance}");
            }
            foreach (var symbol in symbols)
            {
                var balance = await token.GetBalance.CallAsync(new GetBalanceInput
                    {Owner = SideManager.CallAccount, Symbol = symbol});
                Logger.Info($"{SideManager.CallAddress} {symbol}, {balance.Balance}");
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

        private void ProposalThroughController(IMessage input,string method) 
        {
            var token = Genesis.GetTokenContract();
            var association = Genesis.GetAssociationAuthContract();
            var stub = Genesis.GetTokenImplStub();
            var controller =
                AsyncHelper.RunSync(() => stub.GetSideChainRentalControllerCreateInfo.CallAsync(new Empty()));
            var organization = controller.OwnerAddress;
            var controllerInfo = association.GetOrganization(organization);
            var proposer = SideManager.CallAccount;
            controllerInfo.ProposerWhiteList.Proposers.Contains(proposer).ShouldBeTrue();
            
            //create proposal 
            var createProposal = association.CreateProposal(token.ContractAddress, method,
                input, organization, proposer.ToBase58());
            association.Approve(createProposal, proposer.ToBase58());
            
            //create parliament approve proposal
            var parliament = controllerInfo.ProposerWhiteList.Proposers.Where(p => !p.Equals(proposer)).ToList();
            var approveProposal = AuthoritySideManager.ExecuteTransactionWithAuthority(association.ContractAddress,
                nameof(AssociationMethod.Approve), createProposal, proposer.ToBase58(), parliament.First());
            approveProposal.Status.ShouldBe(TransactionResultStatus.Mined);
            
            //check proposal
            var proposalInfo = association.CheckProposal(createProposal);
            proposalInfo.ToBeReleased.ShouldBeTrue();
            
            //release
            var release = association.ReleaseProposal(createProposal, proposer.ToBase58());
            release.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }
    }
}