using AElf.Automation.Common.Helpers;

namespace AElf.Automation.Common.Contracts
{
    public enum DividendsMethod
    {
        GetTermDividends,
        GetTermTotalWeights,
        GetAvailableDividends,
        GetAvailableDividendsByVotingInformation,
        CheckStandardDividends,
        CheckStandardDividendsOfPreviousTerm,
        CheckDividends,
        CheckDividendsOfPreviousTerm
    }

    public class DividendsContract : BaseContract<DividendsMethod>
    {
        public DividendsContract(IApiHelper apiHelper, string callAddress, string dividendsAddress)
            : base(apiHelper, dividendsAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }

        public DividendsContract(IApiHelper apiHelper, string callAddress)
            : base(apiHelper, "AElf.Contracts.Dividends", callAddress)
        {
        }
    }
}