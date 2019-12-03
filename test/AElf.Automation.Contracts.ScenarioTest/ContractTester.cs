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
        public readonly AssociationAuthContract AssociationService;
        public readonly ConsensusContract ConsensusService;
        public readonly ContractServices ContractServices;

        public readonly ElectionContract ElectionService;
        public readonly GenesisContract GenesisService;
        public readonly INodeManager NodeManager;
        public readonly ParliamentAuthContract ParliamentService;
        public readonly ProfitContract ProfitService;
        public readonly ReferendumAuthContract ReferendumService;
        public readonly TokenConverterContract TokenConverterService;
        public readonly TokenContract TokenService;
        public readonly VoteContract VoteService;


        public ContractTester(ContractServices contractServices)
        {
            NodeManager = contractServices.NodeManager;
            ContractServices = contractServices;

            GenesisService = ContractServices.GenesisService;
            ElectionService = ContractServices.ElectionService;
            VoteService = ContractServices.VoteService;
            ProfitService = ContractServices.ProfitService;
            TokenService = ContractServices.TokenService;
            ConsensusService = ContractServices.ConsensusService;
            AssociationService = ContractServices.AssociationAuthService;
            ParliamentService = ContractServices.ParliamentService;
            ReferendumService = ContractServices.ReferendumAuthService;
        }

        public List<string> GetMiners()
        {
            var minerList = new List<string>();
            var miners =
                ConsensusService.CallViewMethod<MinerList>(ConsensusMethod.GetCurrentMinerList, new Empty());
            foreach (var minersPubkey in miners.Pubkeys)
            {
                var miner = Address.FromPublicKey(minersPubkey.ToByteArray());
                minerList.Add(miner.GetFormatted());
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
                if (account == miner || balance > 10_0000000) continue;
                TokenService.SetAccount(ContractServices.CallAddress);
                TokenService.IssueBalance(account, miner, 1000_00000000, symbol);
            }
        }
        
        public void TransferTokenToMiner(string account)
        {
            var symbol = TokenService.GetPrimaryTokenSymbol();
            var miners = GetMiners();
            foreach (var miner in miners)
            {
                var balance = TokenService.GetUserBalance(miner, symbol);
                if (account == miner || balance > 10_0000000) continue;
                TokenService.SetAccount(account);
                TokenService.TransferBalance(account, miner, 1000_00000000, symbol);
            }
        }

        public void TransferToken(string account)
        {
            var symbol = TokenService.GetPrimaryTokenSymbol();
            var balance = TokenService.GetUserBalance(account, symbol);
                if (balance > 10_0000000) return;
                TokenService.SetAccount(TokenService.CallAddress);
                TokenService.TransferBalance(TokenService.CallAddress, account, 1000_00000000, symbol);
        }

        public void IssueToken(string creator, string account)
        {
            var symbol = TokenService.GetPrimaryTokenSymbol();
            var balance = TokenService.GetUserBalance(account, symbol);
            if (balance > 10_0000000) return;
            TokenService.IssueBalance(creator, account, 1000_00000000, symbol);
        }
    }
}