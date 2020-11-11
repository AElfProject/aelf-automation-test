using System.Collections.Generic;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Automation.Contracts.ScenarioTest
{
    public class ContractTester
    {
        public readonly AssociationContract AssociationService;
        public readonly ConfigurationContract ConfigurationService;
        public readonly ConsensusContract ConsensusService;
        public readonly ContractServices ContractServices;
        public readonly CrossChainContract CrossChainService;

        public readonly ElectionContract ElectionService;
        public readonly GenesisContract GenesisService;
        public readonly INodeManager NodeManager;
        public readonly AuthorityManager AuthorityManager;
        public readonly ParliamentContract ParliamentService;
        public readonly ProfitContract ProfitService;
        public readonly ReferendumContract ReferendumService;
        public readonly TokenContract TokenService;
        public readonly VoteContract VoteService;
        public readonly TreasuryContract TreasuryContract;
        public readonly TokenHolderContract TokenHolderContract;
        public readonly TokenConverterContract TokenConverterContract;

        public ContractTester(ContractServices contractServices)
        {
            NodeManager = contractServices.NodeManager;
            ContractServices = contractServices;
            AuthorityManager = new AuthorityManager(NodeManager,contractServices.CallAddress);

            GenesisService = ContractServices.GenesisService;
            ElectionService = ContractServices.ElectionService;
            VoteService = ContractServices.VoteService;
            ProfitService = ContractServices.ProfitService;
            TokenService = ContractServices.TokenService;
            ConsensusService = ContractServices.ConsensusService;
            AssociationService = ContractServices.AssociationService;
            ParliamentService = ContractServices.ParliamentService;
            ReferendumService = ContractServices.ReferendumService;
            ConfigurationService = ContractServices.ConfigurationService;
            CrossChainService = ContractServices.CrossChainService;
            TreasuryContract = ContractServices.TreasuryContract;
            TokenHolderContract = ContractServices.TokenHolderContract;
            TokenConverterContract = ContractServices.TokenConverterService;
        }

        public List<string> GetMiners()
        {
            var minerList = new List<string>();
            var miners =
                ConsensusService.CallViewMethod<MinerList>(ConsensusMethod.GetCurrentMinerList, new Empty());
            foreach (var minersPubkey in miners.Pubkeys)
            {
                var miner = Address.FromPublicKey(minersPubkey.ToByteArray());
                minerList.Add(miner.ToBase58());
            }

            return minerList;
        }

        public void IssueTokenToMiner(string account)
        {
            var symbol = TokenService.GetPrimaryTokenSymbol();
            var miners = GetMiners();
            foreach (var miner in miners)
            {
                var balance = TokenService.GetUserBalance(miner, symbol);
                if (balance > 10000_00000000) continue;
                TokenService.SetAccount(ContractServices.CallAddress);
                TokenService.IssueBalance(account, miner, 20000_00000000, symbol);
            }
        }

        public void TransferTokenToMiner(string account)
        {
            var symbol = TokenService.GetPrimaryTokenSymbol();
            var miners = GetMiners();
            foreach (var miner in miners)
            {
                var balance = TokenService.GetUserBalance(miner, symbol);
                if (account == miner || balance > 200_00000000) continue;
                TokenService.SetAccount(account);
                TokenService.TransferBalance(account, miner, 200_000000000, symbol);
            }
        }

        public void TransferToken(string account)
        {
            var symbol = TokenService.GetPrimaryTokenSymbol();
            var balance = TokenService.GetUserBalance(account, symbol);
            if (balance > 10000_00000000) return;
            TokenService.SetAccount(TokenService.CallAddress);
            TokenService.TransferBalance(TokenService.CallAddress, account, 20000_00000000, symbol);
        }

        public void IssueToken(string creator, string account)
        {
            var symbol = TokenService.GetPrimaryTokenSymbol();
            var balance = TokenService.GetUserBalance(account, symbol);
            if (balance > 10000_00000000) return;
            TokenService.IssueBalance(creator, account, 20000_00000000, symbol);
        }
    }
}