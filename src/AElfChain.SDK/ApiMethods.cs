namespace AElfChain.SDK
{
    public enum ApiMethods
    {
        //Chain
        GetChainInformation,
        DeploySmartContract,
        SendTransaction,
        SendTransactions,
        GetTransactionResult,
        GetTransactionResults,
        GetBlockHeight,
        GetBlockByHeight,
        GetBlockByHash,
        GetBlockState,
        GetChainStatus,
        GetTransactionPoolStatus,
        SendRawTransaction,
        CreateRawTransaction,
        GetContractFileDescriptorSet,
        ExecuteTransaction,
        CurrentRoundInformation,
        TaskQueueStatus,

        //Net
        GetPeers,
        AddPeer,
        RemovePeer
    }
}