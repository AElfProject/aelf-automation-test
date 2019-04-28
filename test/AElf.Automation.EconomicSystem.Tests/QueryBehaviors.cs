using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.Helpers;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Automation.EconomicSystem.Tests
{
    public class QueryBehaviors
    {
        public readonly RpcApiHelper ApiHelper;
        public readonly ContractServices ContractServices;
        
        public readonly ElectionContract ElectionService;
        public readonly VoteContract VoteService;
        public readonly ProfitContract ProfitService;
        public readonly TokenContract TokenService;
        public readonly ConsensusContract ConsensusService;

        public QueryBehaviors(ContractServices contractServices)
        {
            ApiHelper = contractServices.ApiHelper;
            ContractServices = contractServices;

            ElectionService = ContractServices.ElectionService;
            VoteService = ContractServices.VoteService;
            ProfitService = ContractServices.ProfitService;
            TokenService = ContractServices.TokenService;
            ConsensusService = ContractServices.ConsensusService;
        }
        
        #region Election View Methods
        
        public ElectionResult GetElectionResult(long termNumber)
        {
            var electionResult = ElectionService.CallViewMethod<ElectionResult>(ElectionMethod.GetElectionResult,
                new GetElectionResultInput
                {
                    TermNumber = termNumber
                });
            return electionResult;
        }

        public PublicKeysList GetVictories()
        {
            var result =
                ElectionService.CallViewMethod<PublicKeysList>(ElectionMethod.GetVictories, new Empty());
            return result;
        }

        public int GetMinersCount()
        {
            return ElectionService.CallViewMethod<SInt32Value>(ElectionMethod.GetMinersCount, 
                new Empty()).Value;
        }
        
        public CandidateHistory GetCandidateHistory(string publicKey)
        {
            var result =
                ElectionService.CallViewMethod<CandidateHistory>(ElectionMethod.GetCandidateHistory,
                    new StringInput
                    {
                        Value = publicKey
                    });
            return result;
        }

        public Votes GetVotesInformation(string voteAccount)
        {
            var result =
                ElectionService.CallViewMethod<Votes>(ElectionMethod.GetVotesInformation, new StringInput
                {
                    Value = ApiHelper.GetPublicKeyFromAddress(voteAccount)
                });

            return result;
        }

        public Votes GetVotesInformationWithRecords(string voteAccount)
        {
            var result = ElectionService.CallViewMethod<Votes>(ElectionMethod.GetVotesInformationWithRecords,
                new StringInput
                {
                    Value = ApiHelper.GetPublicKeyFromAddress(voteAccount)
                });
            return result;
        }

        public Votes GetVotesInformationWithAllRecords(string voteAccount)
        {
            var result = ElectionService.CallViewMethod<Votes>(ElectionMethod.GetVotesInformationWithAllRecords,
                new StringInput
                {
                    Value = ApiHelper.GetPublicKeyFromAddress(voteAccount)
                });
            return result;
        }
        
        #endregion

        #region VoteService Method

        #endregion

        #region ProfitService Method

        //action
        public CommandInfo ReleaseProfit(long period,int amount,string txId)
        {
            var result =
                ProfitService.ExecuteMethodWithResult(ProfitMethod.ReleaseProfit, new ReleaseProfitInput
                {
                    Period = period,
                    Amount = amount,
                    ProfitId = Hash.LoadHex(txId)
                });
            return result;
        }

        public CommandInfo Profit(string txId)
        {
            var result = ProfitService.ExecuteMethodWithResult(ProfitMethod.Profit, new ProfitInput
            {
                ProfitId = Hash.LoadHex(txId)
            });
            
            return result;
        }
        

        //view
        // return the hash of ProfitService Items(Treasury,MinierReward,BackupSubsidy,CitizaWelfare,BasicReward,VotesWeight,ReElectionReward)
        public CreatedProfitItems GetCreatedProfitItems()
        {
            var result = ProfitService.CallViewMethod<CreatedProfitItems>(ProfitMethod.GetCreatedProfitItems,
                new GetCreatedProfitItemsInput
                {
                    Creator = ContractServices.GenesisService.GetContractAddressByName(NameProvider.ElectionName)
                });
            return result;
        }
        public ProfitItem GetProfitItem(string hex)
        {
            var result = ProfitService.CallViewMethod<ProfitItem>(ProfitMethod.GetProfitItem,
                new Hash()
                {
                    Value = Hash.LoadHex(hex).Value
                });
            return result;
        }

        public Address GetProfitItemVirtualAddress(string hex)
        {
            var result = ProfitService.CallViewMethod<Address>(ProfitMethod.GetProfitItemVirtualAddress,
                new GetProfitItemVirtualAddressInput
                {
                    ProfitId = Hash.LoadHex(hex)
                });

            return result;
        }

        #endregion
        
        #region TokenService Method
        public GetBalanceOutput GetBalance(string account,string symbol = "ELF")
        {
            var balance = TokenService.CallViewMethod<GetBalanceOutput>(TokenMethod.GetBalance, new GetBalanceInput
            {
                Owner = Address.Parse(account),
                Symbol = symbol
            });
            return balance;
        }

        #endregion
    }
}