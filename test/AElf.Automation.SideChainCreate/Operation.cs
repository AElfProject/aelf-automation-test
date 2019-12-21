using System.Collections.Generic;
using System.Linq;
using Acs3;
using Acs7;
using AElf.Contracts.AssociationAuth;
using AElfChain.Common;
using AElfChain.Common.Contracts;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.CrossChain;
using AElf.Contracts.MultiToken;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using ApproveInput = AElf.Contracts.MultiToken.ApproveInput;

namespace AElf.Automation.SideChainCreate
{
    public class Operation
    {
        public readonly ConsensusContract ConsensusService;
        public readonly CrossChainContract CrossChainService;
        public readonly ParliamentAuthContract ParliamentService;
        public readonly string NativeSymbol;


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
                var balance = TokenService.GetUserBalance(miner.GetFormatted());
                if (miner.GetFormatted().Equals(initAccount) || balance > amount) continue;
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
                To = AddressHelper.Base58StringToAddress(Creator)
            });
        }

        public void ApproveToken(long amount)
        {
            //token approve
            TokenService.SetAccount(Creator, Password);
            TokenService.ExecuteMethodWithResult(TokenMethod.Approve,
                new ApproveInput
                {
                    Symbol = NodeOption.NativeTokenSymbol,
                    Spender = AddressHelper.Base58StringToAddress(CrossChainService.ContractAddress),
                    Amount = amount
                });
        }

        public Hash RequestChainCreation(long indexingPrice, long lockedTokenAmount, bool isPrivilegePreserved,
            SideChainTokenInfo tokenInfo)
        {
            CrossChainService.SetAccount(Creator, Password);
            var result =
                CrossChainService.ExecuteMethodWithResult(CrossChainContractMethod.RequestSideChainCreation,
                    new SideChainCreationRequest
                    {
                        IndexingPrice = indexingPrice,
                        LockedTokenAmount = lockedTokenAmount,
                        IsPrivilegePreserved = isPrivilegePreserved,
                        SideChainTokenDecimals = tokenInfo.Decimals,
                        SideChainTokenName = tokenInfo.TokenName,
                        SideChainTokenSymbol = tokenInfo.Symbol,
                        SideChainTokenTotalSupply = tokenInfo.TotalSupply,
                        IsSideChainTokenBurnable = tokenInfo.IsBurnable
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
                ParliamentService.SetAccount(miner.GetFormatted(), "123");
                var result = ParliamentService.ExecuteMethodWithResult(ParliamentMethod.Approve, new Acs3.ApproveInput
                {
                    ProposalId = proposalId
                });
            }
        }

        public int ReleaseSideChainCreation(Hash proposalId)
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
            var creator = sideChainCreatedEvent.Creator;
            return chainId;
        }

        private ContractServices GetContractServices()
        {
            var testEnvironment = ConfigHelper.Config.TestEnvironment;
            var environmentInfo =
                ConfigHelper.Config.EnvironmentInfos.Find(o => o.Environment.Contains(testEnvironment));

            Creator = environmentInfo.Creator;
            Url = environmentInfo.Url;
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