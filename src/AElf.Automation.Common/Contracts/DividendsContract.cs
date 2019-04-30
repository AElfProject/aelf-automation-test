using AElf.Automation.Common.Helpers;
using Google.Protobuf;
using Newtonsoft.Json.Linq;

namespace AElf.Automation.Common.Contracts
{
    public enum DicidendsMethod
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
    public class DividendsContract :BaseContract<DicidendsMethod>
    {
        public DividendsContract(IApiHelper ch, string callAddress, string dividendsAddress)
            :base(ch, dividendsAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }

        public DividendsContract(IApiHelper ch, string callAddress)
            : base(ch, "AElf.Contracts.Dividends", callAddress)
        {
        }
    }
}
