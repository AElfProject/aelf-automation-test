using AElf.Automation.Common.Helpers;

namespace AElf.Automation.RpcPerformance
{
    public interface IPerformanceCategory
    {
        IApiHelper ApiHelper { get; }
        int ThreadCount { get; }
        int ExeTimes { get; }
        string BaseUrl { get; }
        void InitExecCommand(int userCount = 200);
        void DeployContracts();
        void InitializeContracts();
        void PrintContractInfo();
        void ExecuteOneRoundTransactionTask();
        void ExecuteOneRoundTransactionsTask();
        void ExecuteContinuousRoundsTransactionsTask(bool useTxs = false, bool conflict = true);
    }
}