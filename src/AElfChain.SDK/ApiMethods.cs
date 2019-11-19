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
        ExecuteRawTransaction,
        CurrentRoundInformation,
        TaskQueueStatus,
        GetMerklePathByTransactionId,
        GetRoundFromBase64,
        GetMiningSequences,

        //Net
        GetPeers,
        AddPeer,
        RemovePeer,
        NetworkInfo
    }
}