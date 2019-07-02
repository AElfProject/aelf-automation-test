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
        Profit,

        //view
        GetCreatedProfitItems,
        GetProfitItemVirtualAddress,
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

        public void GetProfitItemIds(string electionContractAddress)
        {
            var profitItems = GetCreatedProfitItems(electionContractAddress);
            ProfitItemIds = new Dictionary<ProfitType, Hash>
            {
                {ProfitType.Treasury, profitItems.ProfitIds[0]},
                {ProfitType.MinerReward, profitItems.ProfitIds[1]},
                {ProfitType.BackSubsidy, profitItems.ProfitIds[2]},
                {ProfitType.CitizenWelfare, profitItems.ProfitIds[3]},
                {ProfitType.BasicMinerReward, profitItems.ProfitIds[4]},
                {ProfitType.VotesWeightReward, profitItems.ProfitIds[5]},
                {ProfitType.ReElectionReward, profitItems.ProfitIds[6]}
            };
        }

        public ProfitDetails GetProfitDetails(string voteAddress, Hash profitId)
        {
            var result =
                CallViewMethod<ProfitDetails>(ProfitMethod.GetProfitDetails,
                    new GetProfitDetailsInput
                    {
                        Receiver = Address.Parse(voteAddress),
                        ProfitId = profitId
                    });
            return result;
        }

        public long GetProfitAmount(string account, Hash profitId)
        {
            SetAccount(account);
            return CallViewMethod<SInt64Value>(ProfitMethod.GetProfitAmount, new ProfitInput
            {
                ProfitId = profitId
            }).Value;
        }

        public Address GetTreasuryAddress(Hash profitId, long period = 0)
        {
            return CallViewMethod<Address>(ProfitMethod.GetProfitItemVirtualAddress,
                new GetProfitItemVirtualAddressInput
                {
                    ProfitId = profitId,
                    Period = period
                });
        }

        public Address GetProfitItemVirtualAddress(Hash profitId, long period)
        {
            var result = CallViewMethod<Address>(ProfitMethod.GetProfitItemVirtualAddress,
                new GetProfitItemVirtualAddressInput
                {
                    ProfitId = profitId,
                    Period = period
                });

            return result;
        }

        private CreatedProfitItems GetCreatedProfitItems(string electionContractAddress)
        {
            var result = CallViewMethod<CreatedProfitItems>(ProfitMethod.GetCreatedProfitItems,
                new GetCreatedProfitItemsInput
                {
                    Creator = Address.Parse(electionContractAddress)
                });
            return result;
        }
    }
}