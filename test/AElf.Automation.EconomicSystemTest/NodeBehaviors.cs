using AElf.Client.Dto;
using AElf.Contracts.Election;
using AElfChain.Common.Contracts;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Automation.EconomicSystemTest
{
    public partial class Behaviors
    {
        //action
        public TransactionResultDto AnnouncementElection(string account)
        {
            ElectionService.SetAccount(account);
            return ElectionService.ExecuteMethodWithResult(ElectionMethod.AnnounceElection, new Empty());
        }

        public TransactionResultDto QuitElection(string account)
        {
            ElectionService.SetAccount(account);
            var result = ElectionService.ExecuteMethodWithResult(ElectionMethod.QuitElection, new Empty());
            return result;
        }

        //view
        public CandidateVote GetCandidateVote(string publicKey)
        {
            var candidateVote = ElectionService.CallViewMethod<CandidateVote>(ElectionMethod.GetCandidateVote,
                new StringValue
                {
                    Value = publicKey
                });

            return candidateVote;
        }
    }
}