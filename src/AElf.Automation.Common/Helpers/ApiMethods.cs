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
        BroadcastTransaction,
        BroadcastTransactions,
        GetCommands,
        GetTransactionResult,
        GetTransactionResults,
        GetBlockHeight,
        GetBlockInfo, //rpc
        GetBlockByHeight,
        GetBlockByHash,
        GetMerklePath,
        GetBlockState,
        GetChainStatus,
        GetTransactionPoolStatus,
        SendRawTransaction,
        CreateRawTransaction,
        GetContractFileDescriptorSet,
        QueryView,
        Call,

        //Net
        GetPeers,
        AddPeer,
        RemovePeer
    }
}