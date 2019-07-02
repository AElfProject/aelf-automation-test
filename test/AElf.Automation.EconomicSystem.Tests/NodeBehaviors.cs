using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Contracts.Election;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Automation.EconomicSystem.Tests
{
    public partial class Behaviors
    {
        //action
        public CommandInfo AnnouncementElection(string account)
        {
            ElectionService.SetAccount(account);
            return ElectionService.ExecuteMethodWithResult(ElectionMethod.AnnounceElection, new Empty());
        }

        public CommandInfo QuitElection(string account)
        {
            ElectionService.SetAccount(account);
            var result = ElectionService.ExecuteMethodWithResult(ElectionMethod.QuitElection, new Empty());
            return result;
        }

        //view
        public CandidateVote GetCandidateVote(string publicKey)
        {
            var candidateVote = ElectionService.CallViewMethod<CandidateVote>(ElectionMethod.GetCandidateVote, new StringInput
            {
                Value = publicKey
            });

            return candidateVote;
        }
    }
}