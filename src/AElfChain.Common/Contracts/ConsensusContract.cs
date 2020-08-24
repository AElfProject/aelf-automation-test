using System.Collections.Generic;
using System.Linq;
using Acs1;
using Acs10;
using AElf;
using AElf.Contracts.Consensus.AEDPoS;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;

namespace AElfChain.Common.Contracts
{
    public enum ConsensusMethod
    {
        GetRoundInformation,
        GetCurrentTermNumber,
        SetMaximumMinersCount,
        IsCandidate,
        GetVotesCount,
        GetTicketsCount,
        GetCandidatesList,
        GetCandidateHistoryInformation,
        GetCurrentMinerList,
        GetCurrentRoundInformation,
        GetCurrentMinerPubkeyList,
        GetTicketsInfo,
        GetPageableElectionInfo,
        GetBlockchainAge,
        GetCurrentVictories,
        GetTermSnapshot,
        GetTermNumberByRoundNumber,
        QueryAliasesInUse,
        QueryCurrentDividends,
        QueryCurrentDividendsForVoters,
        QueryMinedBlockCountInCurrentTerm,
        GetAvailableDividends,
        ContributeToSideChainDividendsPool,
        GetMaximumBlocksCount,
        GetMaximumMinersCount,
        GetMaximumMinersCountController,
        GetUndistributedDividends,
        GetSymbolList,
        GetDividends,

        AnnounceElection,
        QuitElection,
        Vote,
        ReceiveAllDividends,
        WithdrawAll,
        InitialBalance,
        ChangeMaximumMinersCountController,
        Donate
    }

    public class ConsensusContract : BaseContract<ConsensusMethod>
    {
        public ConsensusContract(INodeManager nodeManager, string callAddress, string consensusAddress)
            : base(nodeManager, consensusAddress)
        {
            SetAccount(callAddress);
        }

        public long GetCurrentTermInformation()
        {
            var round = CallViewMethod<Round>(ConsensusMethod.GetCurrentRoundInformation, new Empty());

            return round.TermNumber;
        }

        public List<string> GetCurrentMinersPubkey()
        {
            var miners = CallViewMethod<MinerList>(ConsensusMethod.GetCurrentMinerList, new Empty());
            return miners.Pubkeys.Select(o => o.ToByteArray().ToHex()).ToList();
        }

        public List<string> GetInitialMinersPubkey()
        {
            var roundInfo = CallViewMethod<Round>(ConsensusMethod.GetRoundInformation, new Int64Value
            {
                Value = 1
            });
            return roundInfo.RealTimeMinersInformation.Keys.ToList();
        }

        public Int32Value GetMaximumMinersCount()
        {
            return CallViewMethod<Int32Value>(ConsensusMethod.GetMaximumMinersCount, new Empty());
        }
        
        public AuthorityInfo GetMaximumMinersCountController()
        {
            return CallViewMethod<AuthorityInfo>(ConsensusMethod.GetMaximumMinersCountController, new Empty());
        }

        public Dividends GetUndistributedDividends()
        {
            var unAmount = CallViewMethod<Dividends>(ConsensusMethod.GetUndistributedDividends, new Empty());
            Logger.Info($"UndistributedDividends amount:{unAmount}");
            return unAmount;
        }
        
        public Dividends GetDividends()
        {
            var amount = CallViewMethod<Dividends>(ConsensusMethod.GetDividends, new Empty());
            Logger.Info($"Dividends amount:{amount}");
            return amount;
        }
        
        public SymbolList GetSymbolList()
        {
            var check = CallViewMethod<SymbolList>(ConsensusMethod.GetSymbolList, new Empty());
            Logger.Info($"Symbol list:{check}");
            return check;
        }
    }
}