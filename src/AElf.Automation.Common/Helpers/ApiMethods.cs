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
        GetTransactionsResult,
        GetBlockHeight,
        GetBlockInfo,
        GetMerklePath,
        QueryView,

        //Net
        GetPeers,
        AddPeer,
        RemovePeer
    }
}