using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Profit;
using AElf.Contracts.Treasury;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;

namespace AElfChain.Common.Contracts
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
        ClaimProfitsByPeriod,

        //view
        GetManagingSchemeIds,
        GetSchemeAddress,
        GetScheme,
        GetReleasedProfitsInformation,
        GetProfitDetails,
        GetProfitsMap,
        GetProfitItem,
        GetProfitAmount,
        GetDistributedProfitsInfo
    }

    public enum SchemeType
    {
        Treasury,

        MinerReward, //2
        BackupSubsidy,//0.5
        CitizenWelfare,//7.5
        
//MinerReward: MinerBasicReward,VotesWeightReward,ReElectionReward
        MinerBasicReward,//1
        WelcomeReward,//0.5
        FlexibleReward//0.5
    }

    public class ProfitContract : BaseContract<ProfitMethod>
    {
        public ProfitContract(INodeManager nodeManager, string callAddress) :
            base(nodeManager, "AElf.Contracts.Profit", callAddress)
        {
        }

        public ProfitContract(INodeManager nodeManager, string callAddress, string contractAddress) :
            base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }

        public static Dictionary<SchemeType, Scheme> Schemes { get; set; }

        public void GetTreasurySchemes(string treasuryContractAddress)
        {
            if (Schemes != null && Schemes.Count == 7)
                return;
            Schemes = new Dictionary<SchemeType, Scheme>();
            var treasuryContract = new TreasuryContract(NodeManager, CallAddress, treasuryContractAddress);
            var treasurySchemeId =
                treasuryContract.CallViewMethod<Hash>(TreasuryMethod.GetTreasurySchemeId, new Empty());
            var treasuryScheme = CallViewMethod<Scheme>(ProfitMethod.GetScheme, treasurySchemeId);
            Schemes.Add(SchemeType.Treasury, treasuryScheme);
            var dividendPoolWeightProportion =
                treasuryContract.CallViewMethod<DividendPoolWeightProportion>(
                    TreasuryMethod.GetDividendPoolWeightProportion, new Empty());
            var minerRewardScheme =
                CallViewMethod<Scheme>(ProfitMethod.GetScheme,
                    treasuryScheme.SubSchemes.First(o =>
                        o.SchemeId == dividendPoolWeightProportion.MinerRewardProportionInfo.SchemeId).SchemeId);
            Schemes.Add(SchemeType.MinerReward, minerRewardScheme);

            Schemes.Add(SchemeType.BackupSubsidy,
                CallViewMethod<Scheme>(ProfitMethod.GetScheme,
                    treasuryScheme.SubSchemes.First(o =>
                        o.SchemeId == dividendPoolWeightProportion.BackupSubsidyProportionInfo.SchemeId).SchemeId));
            Schemes.Add(SchemeType.CitizenWelfare,
                CallViewMethod<Scheme>(ProfitMethod.GetScheme,
                    treasuryScheme.SubSchemes.First(o =>
                        o.SchemeId == dividendPoolWeightProportion.CitizenWelfareProportionInfo.SchemeId).SchemeId));

            Schemes.Add(SchemeType.MinerBasicReward,
                CallViewMethod<Scheme>(ProfitMethod.GetScheme, minerRewardScheme.SubSchemes[0].SchemeId));
            Schemes.Add(SchemeType.WelcomeReward,
                CallViewMethod<Scheme>(ProfitMethod.GetScheme, minerRewardScheme.SubSchemes[1].SchemeId));
            Schemes.Add(SchemeType.FlexibleReward,
                CallViewMethod<Scheme>(ProfitMethod.GetScheme, minerRewardScheme.SubSchemes[2].SchemeId));
            Logger.Info("Scheme collection info:");
            foreach (var (key, value) in Schemes) Logger.Info($"Name: {key}, SchemeId: {value.SchemeId}");
        }

        public Scheme GetScheme(Hash hash)
        {
            var scheme =
                CallViewMethod<Scheme>(ProfitMethod.GetScheme,hash);
            return scheme;
        }

        public ProfitDetails GetProfitDetails(string voteAddress, Hash profitId)
        {
            var result =
                CallViewMethod<ProfitDetails>(ProfitMethod.GetProfitDetails,
                    new GetProfitDetailsInput
                    {
                        Beneficiary = voteAddress.ConvertAddress(),
                        SchemeId = profitId
                    });
            return result;
        }

        public long GetProfitAmount(string account, Hash schemeId)
        {
            var newTester = GetNewTester(account);
            return newTester.CallViewMethod<Int64Value>(ProfitMethod.GetProfitAmount, new ClaimProfitsInput
            {
                SchemeId = schemeId,
                Beneficiary = account.ConvertAddress()
            }).Value;
        }
        
        public ReceivedProfitsMap GetProfitsMap(string account, Hash schemeId)
        {
            var profitsMap = CallViewMethod<ReceivedProfitsMap>(ProfitMethod.GetProfitsMap, new ClaimProfitsInput
            {
                SchemeId = schemeId,
                Beneficiary = account.ConvertAddress()
            });
            return profitsMap;
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
    }
}