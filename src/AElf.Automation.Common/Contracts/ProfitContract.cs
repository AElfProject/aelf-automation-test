using System.Collections.Generic;
using AElf.Automation.Common.Helpers;
using AElf.Contracts.Profit;
using AElf.Types;

namespace AElf.Automation.Common.Contracts
{
    public enum ProfitMethod
    {
        //action
        InitializeProfitContract,
        CreateProfitItem,
        RegisterSubProfitItem,
        AddWeight,
        SubWeight,
        AddWeights,
        ReleaseProfit,
        AddProfits,
        ClaimProfits,

        //view
        GetCreatedSchemeIds,
        GetSchemeAddress,
        GetReleasedProfitsInformation,
        GetProfitDetails,
        GetProfitItem,
        GetProfitAmount
    }

    public enum ProfitType
    {
        Treasury,
        MinerReward,
        BackSubsidy,
        CitizenWelfare,
        BasicMinerReward,
        VotesWeightReward,
        ReElectionReward
    }

    public class ProfitContract : BaseContract<ProfitMethod>
    {
        public Dictionary<ProfitType, Hash> ProfitItemIds { get; set; }

        public ProfitContract(IApiHelper apiHelper, string callAddress) :
            base(apiHelper, "AElf.Contracts.Profit", callAddress)
        {
        }

        public ProfitContract(IApiHelper apiHelper, string callAddress, string contractAddress) :
            base(apiHelper, contractAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }

        public void GetProfitItemIds(string treasuryContractAddress)
        {
            var profitIds = GetCreatedProfitItems(treasuryContractAddress).SchemeIds;
            ProfitItemIds = new Dictionary<ProfitType, Hash>
            {
                {ProfitType.Treasury, profitIds[0]},
                {ProfitType.MinerReward, profitIds[1]},
                {ProfitType.BackSubsidy, profitIds[2]},
                {ProfitType.CitizenWelfare, profitIds[3]},
                {ProfitType.BasicMinerReward, profitIds[4]},
                {ProfitType.VotesWeightReward, profitIds[5]},
                {ProfitType.ReElectionReward, profitIds[6]}
            };
        }

        public ProfitDetails GetProfitDetails(string voteAddress, Hash profitId)
        {
            var result =
                CallViewMethod<ProfitDetails>(ProfitMethod.GetProfitDetails,
                    new GetProfitDetailsInput
                    {
                        Beneficiary = Address.Parse(voteAddress),
                        SchemeId = profitId
                    });
            return result;
        }

        public long GetProfitAmount(string account, Hash schemeId)
        {
            var newTester = GetNewTester(account);
            return newTester.CallViewMethod<SInt64Value>(ProfitMethod.GetProfitAmount, new ClaimProfitsInput
            {
                SchemeId = schemeId,
                Symbol = "ELF"
            }).Value;
        }

        public Address GetSchemeAddress(Hash schemeId, long period)
        {
            var result = CallViewMethod<Address>(ProfitMethod.GetSchemeAddress,
                new SchemePeriod
                {
                    SchemeId = schemeId,
                    Period = period
                });

            return result;
        }

        private CreatedSchemeIds GetCreatedProfitItems(string treasuryContractAddress)
        {
            var result = CallViewMethod<CreatedSchemeIds>(ProfitMethod.GetCreatedSchemeIds,
                new GetCreatedSchemeIdsInput
                {
                    Creator = Address.Parse(treasuryContractAddress)
                });
            return result;
        }
    }
}