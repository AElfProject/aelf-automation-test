using System.Collections.Generic;
using System.Linq;
using Acs3;
using AElf.Contracts.Association;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using Volo.Abp.Threading;

namespace AElf.Automation.E2ETest
{
    public class ContractTestBase
    {
        public static string MainConfig = "nodes-env1-main";
        public static string SideConfig = "nodes-env2-side1";
        public static Address AssociationOrganization;
        public static Address ReferendumOrganization;

        public ContractTestBase()
        {
            Log4NetHelper.LogInit("ContractTest");
            Logger = Log4NetHelper.GetLogger();

            NodeInfoHelper.SetConfig(MainConfig);
            ConfigNodes = NodeInfoHelper.Config.Nodes;
            var firstBp = ConfigNodes.First();

            NodeManager = new NodeManager(firstBp.Endpoint);
            ContractManager = new ContractManager(NodeManager, firstBp.Account);
            EnvCheck = EnvCheck.GetDefaultEnvCheck();
            TransferToNodes();
            AssociationOrganization = CreateAssociationOrganization();
            ReferendumOrganization = CreateReferendumOrganization();
        }

        public INodeManager NodeManager { get; set; }
        public ContractManager ContractManager { get; set; }
        public EnvCheck EnvCheck { get; set; }
        public ILog Logger { get; set; }

        public List<Node> ConfigNodes { get; set; }

        public void TransferToNodes()
        {
            foreach (var node in ConfigNodes)
            {
                var symbol = ContractManager.Token.GetPrimaryTokenSymbol();
                var balance = ContractManager.Token.GetUserBalance(node.Account, symbol);
                if (node.Account.Equals(ContractManager.CallAddress) || balance > 10000000000) continue;
                ContractManager.Token.TransferBalance(ContractManager.CallAddress, node.Account, 100000000000,
                    symbol);
            }
        }

        private Address CreateAssociationOrganization()
        {
             var miners = ContractManager.Authority.GetCurrentMiners();
            var association = ContractManager.Association;
            var members = ConfigNodes.Select(l => l.Account).ToList().Select(member => member.ConvertAddress()).Take(3);
            //create association organization
            var enumerable = members as Address[] ?? members.ToArray();
            var createInput = new CreateOrganizationInput
            {
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MaximalAbstentionThreshold = 1,
                    MaximalRejectionThreshold = 1,
                    MinimalApprovalThreshold = 2,
                    MinimalVoteThreshold = 2
                },
                ProposerWhiteList = new ProposerWhiteList {Proposers = {miners.First().ConvertAddress()}},
                OrganizationMemberList = new OrganizationMemberList {OrganizationMembers = {enumerable}}
            };
            association.SetAccount(miners.First());
            var result = association.ExecuteMethodWithResult(AssociationMethod.CreateOrganization,
                createInput);
            var organizationAddress =
                Address.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            return organizationAddress;
        }
        
        private Address CreateReferendumOrganization()
        {
            var proposer = ConfigNodes.First().Account.ConvertAddress();
            var referendum = ContractManager.Referendum;
            //create referendum organization
            var createInput = new AElf.Contracts.Referendum.CreateOrganizationInput
            {
                TokenSymbol = "ELF",
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MaximalAbstentionThreshold = 1000,
                    MaximalRejectionThreshold = 1000,
                    MinimalApprovalThreshold = 2000,
                    MinimalVoteThreshold = 2000
                },
                ProposerWhiteList = new ProposerWhiteList {Proposers = {proposer}}
            };
            var result = referendum.ExecuteMethodWithResult(ReferendumMethod.CreateOrganization,
                createInput);
            var organizationAddress =
                Address.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            return organizationAddress;
        }
    }
}