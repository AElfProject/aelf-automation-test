using AElf.Client.Dto;
using AElf.Contracts.Election;
using AElf.Types;
using AElfChain.Common.Contracts;
using AElfChain.Common.DtoExtension;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Automation.EconomicSystemTest
{
    public partial class Behaviors
    {
        //action
        public TransactionResultDto AnnouncementElection(string announceAccount, string admin = null )
        {
            if (admin == null)
                admin = announceAccount;
            ElectionService.SetAccount(announceAccount);
            return ElectionService.ExecuteMethodWithResult(ElectionMethod.AnnounceElection, admin.ConvertAddress());
        }

        public TransactionResultDto QuitElection(string admin,string quitAccount)
        {
            ElectionService.SetAccount(admin);
            var pubkey = NodeManager.GetAccountPublicKey(quitAccount);
            var result = ElectionService.ExecuteMethodWithResult(ElectionMethod.QuitElection, new StringValue{Value = pubkey});
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