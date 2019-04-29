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
        QueryView,

        //Net
        GetPeers,
        AddPeer,
        RemovePeer
    }
}