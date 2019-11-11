using AElfChain.Common.Managers;

namespace AElfChain.Common.Contracts
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
        public DividendsContract(INodeManager nodeManager, string callAddress, string dividendsAddress)
            : base(nodeManager, dividendsAddress)
        {
            SetAccount(callAddress);
        }

        public DividendsContract(INodeManager nodeManager, string callAddress)
            : base(nodeManager, ContractFileName, callAddress)
        {
        }

        public static string ContractFileName => "AElf.Contracts.Dividends";
    }
}