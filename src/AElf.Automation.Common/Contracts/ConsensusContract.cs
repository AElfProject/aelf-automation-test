using System.Collections.Generic;
using System.Linq;
using AElf.Automation.Common.Helpers;
using AElf.Contracts.Consensus.AEDPoS;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Automation.Common.Contracts
{
    public enum ConsensusMethod
    {
        GetRoundInfo,
        GetCurrentTermNumber,
        IsCandidate,
        GetVotesCount,
        GetTicketsCount,
        GetCandidatesList,
        GetCandidateHistoryInformation,
        GetCurrentMinerList,
        GetCurrentRoundInformation,
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

        AnnounceElection,
        QuitElection,
        Vote,
        ReceiveAllDividends,
        WithdrawAll,
        InitialBalance
    }

    public class ConsensusContract : BaseContract<ConsensusMethod>
    {
        public ConsensusContract(IApiHelper apiHelper, string callAddress, string consensusAddress) :
            base(apiHelper, consensusAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }

        public ConsensusContract(IApiHelper apiHelper, string callAddress)
            : base(apiHelper, "AElf.Contracts.Consensus", callAddress)
        {
        }

        public long GetCurrentTermInformation()
        {
            var round = CallViewMethod<Round>(ConsensusMethod.GetCurrentRoundInformation, new Empty());

            return round.TermNumber;
        }
        
        public List<string> GetCurrentMiners()
        {
            var miners = CallViewMethod<MinerList>(ConsensusMethod.GetCurrentMinerList, new Empty());
            return miners.Pubkeys.Select(o => o.ToByteArray().ToHex()).ToList();
        }
    }
}