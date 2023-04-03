using System.Collections.Generic;
using System.Linq;
using AElf.Standards.ACS1;
using AElf.Standards.ACS3;
using AElf.Standards.ACS7;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.CrossChain;
using AElf.Contracts.MultiToken;
using AElf.Types;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Automation.SideChainCreate
{
    public class Operation
    {
        public readonly ConsensusContract ConsensusService;
        public readonly CrossChainContract CrossChainService;
        public readonly string NativeSymbol;
        public readonly ParliamentContract ParliamentService;
        public readonly TokenContract TokenService;
        public string Creator;
        public string Password;

        public string Url;

        public Operation()
        {
            var contractServices = GetContractServices();
            TokenService = contractServices.TokenService;
            CrossChainService = contractServices.CrossChainService;
            ParliamentService = contractServices.ParliamentService;
            ConsensusService = contractServices.ConsensusService;
            NativeSymbol = TokenService.GetPrimaryTokenSymbol();
        }

        public void TransferToken(long amount)
        {
            TokenService.SetAccount(Creator, Password);
            var miners = GetMiners();
            var initAccount = NodeOption.AllNodes.First().Account;
            var password = NodeOption.AllNodes.First().Password;
            TokenService.SetAccount(initAccount, password);

            foreach (var miner in miners)
            {
                var balance = TokenService.GetUserBalance(miner.ToBase58());
                if (miner.ToBase58().Equals(initAccount) || balance > amount) continue;
                TokenService.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
                {
                    Symbol = NativeSymbol,
                    Amount = amount,
                    Memo = "Transfer to miners",
                    To = miner
                });
            }

            var creatorBalance = TokenService.GetUserBalance(Creator);
            if (Creator.Equals(initAccount) || creatorBalance > amount) return;
            TokenService.ExecuteMethodWithResult(TokenMethod.Transfer, new TransferInput
            {
                Symbol = NativeSymbol,
                Amount = amount,
                Memo = "Transfer to creator",
                To = Creator.ConvertAddress()
            });
        }

        public void ApproveToken(long amount)
        {
            //token approve
            if (amount == 0)
                return;
            TokenService.SetAccount(Creator, Password);
            TokenService.ExecuteMethodWithResult(TokenMethod.Approve,
                new ApproveInput
                {
                    Symbol = NodeOption.NativeTokenSymbol,
                    Spender = CrossChainService.Contract,
                    Amount = amount
                });
        }

        public Hash RequestChainCreation(long indexingPrice, long lockedTokenAmount, bool isPrivilegePreserved,
            SideChainTokenCreationRequest sideChainTokenCreationRequest)
        {
            CrossChainService.SetAccount(Creator, Password);
            var sideChainTokenInitialIssue = new SideChainTokenInitialIssue
                {Address = Creator.ConvertAddress(), Amount = 1000_0000_00000000};
            var result =
                CrossChainService.ExecuteMethodWithResult(CrossChainContractMethod.RequestSideChainCreation,
                    new SideChainCreationRequest
                    {
                        IndexingPrice = indexingPrice,
                        LockedTokenAmount = lockedTokenAmount,
                        IsPrivilegePreserved = isPrivilegePreserved,
                        SideChainTokenCreationRequest = sideChainTokenCreationRequest,
                        InitialResourceAmount = {{"CPU", 2}, {"RAM", 4}, {"DISK", 512}, {"NET", 1024}},
                        SideChainTokenInitialIssueList = {sideChainTokenInitialIssue}
                    });
            var proposalId = ProposalCreated.Parser
                .ParseFrom(ByteString.FromBase64(result.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                    .NonIndexed)).ProposalId;
            return proposalId;
        }

        public void ApproveProposal(Hash proposalId)
        {
            var miners = GetMiners();
            foreach (var miner in miners)
            {
                ParliamentService.SetAccount(miner.ToBase58(), "123");
                var result = ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Approve, proposalId);
            }
        }

        public int ReleaseSideChainCreation(Hash proposalId, out Address organization)
        {
            CrossChainService.SetAccount(Creator, Password);
            var result
                = CrossChainService.ExecuteMethodWithResult(CrossChainContractMethod.ReleaseSideChainCreation,
                    new ReleaseSideChainCreationInput
                    {
                        ProposalId = proposalId
                    });
            var release = result.Logs.First(l => l.Name.Contains(nameof(SideChainCreatedEvent)))
                .NonIndexed;
            var byteString = ByteString.FromBase64(release);
            var sideChainCreatedEvent = SideChainCreatedEvent.Parser
                .ParseFrom(byteString);
            var chainId = sideChainCreatedEvent.ChainId;
            if (result.Logs.FirstOrDefault(l => l.Name.Contains(nameof(OrganizationCreated))) == null)
            {
                var controller = CrossChainService.CallViewMethod<AuthorityInfo>(
                    CrossChainContractMethod.GetSideChainLifetimeController,
                    new Empty());
                organization = controller.OwnerAddress;
            }
            else
            {
                organization = OrganizationCreated.Parser
                    .ParseFrom(ByteString.FromBase64(result.Logs
                        .First(l => l.Name.Contains(nameof(OrganizationCreated)))
                        .NonIndexed)).OrganizationAddress;
            }

            return chainId;
        }

        private ContractServices GetContractServices()
        {
            var testEnvironment = ConfigHelper.Config.TestEnvironment;
            var environmentInfo =
                ConfigHelper.Config.EnvironmentInfos.Find(o => o.Environment.Contains(testEnvironment));

            Creator = environmentInfo.Creator;
            Url = NodeOption.AllNodes.First().Endpoint;
            Password = environmentInfo.Password;
            var contractService = new ContractServices(Url, Creator, Password);
            return contractService;
        }

        private IEnumerable<Address> GetMiners()
        {
            var minerList = new List<Address>();
            var miners = ConsensusService.CallViewMethod<MinerList>(ConsensusMethod.GetCurrentMinerList, new Empty());
            foreach (var publicKey in miners.Pubkeys)
            {
                var address = Address.FromPublicKey(publicKey.ToByteArray());
                minerList.Add(address);
            }

            return minerList;
        }
    }
}