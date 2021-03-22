using AElfChain.Common.Managers;

namespace AElf.Automation.RpcPerformance
{
    public interface IPerformanceCategory
    {
        INodeManager NodeManager { get; }
        int ThreadCount { get; }
        int ExeTimes { get; }
        string BaseUrl { get; }
        void InitExecCommand(int userCount = 150);
        void DeployContracts();
        void InitializeMainContracts();
        void PrintContractInfo();
        void ExecuteContinuousRoundsTransactionsTask(bool useTxs = false);
    }
}