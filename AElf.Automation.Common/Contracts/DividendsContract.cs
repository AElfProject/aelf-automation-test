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

        public CommandInfo CallContractMethod(DicidendsMethod method, IMessage inputParameter)
        {
            return ExecuteContractMethodWithResult(method.ToString(), inputParameter);
        }

        public void CallContractWithoutResult(DicidendsMethod method, IMessage inputParameter)
        {
            ExecuteContractMethod(method.ToString(), inputParameter);
        }

        public JObject CallReadOnlyMethod(DicidendsMethod method, IMessage inputParameter)
        {
            return CallContractViewMethod(method.ToString(), inputParameter);
        }
    }
}
