namespace AElf.Automation.Common.Helpers
{
    public enum ApiMethods
    {
        //Wallet
        AccountNew,
        AccountList,
        AccountUnlock,

        //Chain
        ConnectChain,
        LoadContractAbi,
        DeployContract,
        BroadcastTx,
        BroadcastTxs,
        GetCommands,
        GetContractAbi,
        GetIncrement,
        GetTxResult,
        GetTxsResult,
        GetBlockHeight,
        GetBlockInfo,
        GetMerklePath,
        SetBlockVolumn,

        //Net
        GetPeers,
        AddPeer,
        RemovePeer
    }
}