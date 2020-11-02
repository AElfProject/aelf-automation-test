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
        void DeployContractsWithAuthority(bool isOnlyDeploy);
        void SideChainDeployContractsWithCreator(bool isOnlyDeploy);
        void SideChainDeployContractsWithAuthority(bool isOnlyDeploy);
        void DeployContracts();
        void InitializeMainContracts();
        void InitializeSideChainToken();
        void PrintContractInfo();
        void ExecuteOneRoundTransactionTask();
        void ExecuteOneRoundTransactionsTask();
        void ExecuteContinuousRoundsTransactionsTask(bool useTxs = false);
    }
}