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
        public DividendsContract(CliHelper ch, string callAddress, string dividendsAbi)
            :base(ch, dividendsAbi)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }

        public DividendsContract(CliHelper ch, string callAddress)
            : base(ch, "AElf.Contracts.Dividends", callAddress)
        {
        }

        public CommandInfo CallContractMethod(DicidendsMethod method, IMessage inputParameter)
        {
            return ExecuteMethodWithResult(method.ToString(), inputParameter);
        }

        public void CallContractWithoutResult(DicidendsMethod method, IMessage inputParameter)
        {
            ExecuteMethodWithTxId(method.ToString(), inputParameter);
        }

        public JObject CallReadOnlyMethod(DicidendsMethod method, IMessage inputParameter)
        {
            return CallViewMethod(method.ToString(), inputParameter);
        }
    }
}
