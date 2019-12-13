using AElfChain.Common.Managers;

namespace AElf.Automation.RpcPerformance
{
    public interface IPerformanceCategory
    {
        INodeManager NodeManager { get; }
        int ThreadCount { get; }
        int ExeTimes { get; }
        string BaseUrl { get; }
        void InitExecCommand(int userCount = 200);
        void DeployContractsWithAuthority();
        void SideChainDeployContractsWithCreator();
        void SideChainDeployContractsWithAuthority();
        void DeployContracts();
        void InitializeContracts();
        void PrintContractInfo();
        void ExecuteOneRoundTransactionTask();
        void ExecuteOneRoundTransactionsTask();
        void ExecuteContinuousRoundsTransactionsTask(bool useTxs = false);
    }
}