namespace AElf.Automation.Common.Helpers
{
    public enum ApiMethods
    {
        //Wallet
        AccountNew,
        AccountList,
        AccountUnlock,

        //Chain
        GetChainInformation,
        DeploySmartContract,
        SendTransaction,
        SendTransactions,
        GetTransactionResult,
        GetTransactionResults,
        GetBlockHeight,
        GetBlockInfo, //rpc
        GetBlockByHeight,
        GetBlockByHash,
        GetBlockState,
        GetChainStatus,
        GetTransactionPoolStatus,
        SendRawTransaction,
        CreateRawTransaction,
        GetContractFileDescriptorSet,
        QueryView,
        ExecuteTransaction,
        CurrentRoundInformation,
        TaskQueueStatus,

        //Net
        GetPeers,
        AddPeer,
        RemovePeer
    }
}