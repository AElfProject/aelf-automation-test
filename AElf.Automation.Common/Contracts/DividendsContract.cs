using AElf.Automation.Common.Helpers;
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
    public class DividendsContract :BaseContract
    {
        public DividendsContract(CliHelper ch, string account, string dividendsAbi)
            :base(ch, dividendsAbi)
        {
            Account = account;
            UnlockAccount(Account);
        }

        public DividendsContract(CliHelper ch, string account)
            : base(ch, "AElf.Contracts.Dividends", account)
        {
        }

        public CommandInfo CallContractMethod(DicidendsMethod method, params string[] paramsArray)
        {
            return ExecuteContractMethodWithResult(method.ToString(), paramsArray);
        }

        public void CallContractWithoutResult(DicidendsMethod method, params string[] paramsArray)
        {
            ExecuteContractMethod(method.ToString(), paramsArray);
        }

        public JObject CallReadOnlyMethod(DicidendsMethod method, params string[] paramsArray)
        {
            return CallContractViewMethod(method.ToString(), paramsArray);
        }
    }
}
