using System.Collections.Generic;
using System.Linq;
using AElf.Standards.ACS3;
using AElf.Contracts.Association;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using log4net;
using Shouldly;

namespace AElf.Automation.AssociationTest
{
    public class AddMembers
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        public AddMembers(INodeManager nodeManager, string caller)
        {
            NodeManager = nodeManager;
            Caller = caller;
            Genesis = nodeManager.GetGenesisContract(Caller);
            Association = Genesis.GetAssociationAuthContract();
            Token = Genesis.GetTokenContract();
        }
        
        private INodeManager NodeManager { get; }
        private GenesisContract Genesis { get; }
        private TokenContract Token { get; }
        private AssociationContract Association { get; }

        private string Caller { get; }
        
        public Address CreateOrganization(string token)
        {
            var enumerable = Members.ToList();
            var addresses = enumerable.Select(o => o.ConvertAddress()).ToList();
            var createInput = new CreateOrganizationInput
            {
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MaximalAbstentionThreshold = 1,
                    MaximalRejectionThreshold = 1,
                    MinimalApprovalThreshold = 2,
                    MinimalVoteThreshold = 3
                },
                ProposerWhiteList = new ProposerWhiteList {Proposers = {Caller.ConvertAddress()}},
                OrganizationMemberList = new OrganizationMemberList {OrganizationMembers = {addresses}},
                CreationToken = HashHelper.ComputeFrom(token)
            };
            var result = Association.ExecuteMethodWithResult(AssociationMethod.CreateOrganization,
                createInput);
            var organizationAddress =
                Address.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result.ReturnValue));
            return organizationAddress;
        }

        public void AddMemberTest(string organization)
        {
            var address = NodeManager.AccountManager.NewAccount();
            var input = address.ConvertAddress();
            var createProposal = Association.CreateProposal(Association.ContractAddress,
                nameof(AssociationMethod.AddMember), input, organization.ConvertAddress(), Caller);
            var reviewers = Association.GetOrganization(organization.ConvertAddress()).OrganizationMemberList.OrganizationMembers;
            foreach (var member in reviewers)
            {
                var proposalInfo = Association.CheckProposal(createProposal);
                if (!proposalInfo.ToBeReleased)
                    Association.ApproveProposal(createProposal,member.ToBase58());
            }
            var release = Association.ReleaseProposal(createProposal, Caller);
            release.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            reviewers = Association.GetOrganization(organization.ConvertAddress()).OrganizationMemberList.OrganizationMembers;
            reviewers.ShouldContain(input);
            CheckOrganization(organization);
        }

        public void TransferToMember(string organization)
        {
            var reviewers = Association.GetOrganization(organization.ConvertAddress());
            foreach (var reviewer in reviewers.OrganizationMemberList.OrganizationMembers)
            {
                var balance = Token.GetUserBalance(reviewer.ToBase58());
                if (balance > 10_00000000)
                    continue;
                Token.TransferBalance(Caller, reviewer.ToBase58(), 1000_00000000);
            }
        }

        public void CheckOrganization(string organization)
        {
            var info = Association.GetOrganization(organization.ConvertAddress());
            var members = info.OrganizationMemberList.OrganizationMembers;
            Logger.Info($"{organization} members : {members}");
        }

        public static string[] Members =
        {
            "2oSMWm1tjRqVdfmrdL8dgrRvhWu1FP8wcZidjS6wPbuoVtxhEz",
            "WRy3ADLZ4bEQTn86ENi5GXi5J1YyHp9e99pPso84v2NJkfn5k",
            "2frDVeV6VxUozNqcFbgoxruyqCRAuSyXyfCaov6bYWc7Gkxkh2"
        };
    }
}