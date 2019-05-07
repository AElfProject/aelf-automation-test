using Google.Protobuf;
using Newtonsoft.Json.Linq;

namespace AElf.Automation.Common.Helpers
{
    public enum ApiType
    {
        RpcApi,
        WebApi
    }
    public interface IApiHelper
    {
        ApiType GetApiType();
        string GetGenesisContractAddress();
        CommandInfo ExecuteCommand(CommandInfo ci);
        //account
        CommandInfo NewAccount(CommandInfo ci);
        CommandInfo ListAccounts();
        CommandInfo UnlockAccount(CommandInfo ci);
        
        //chain
        void RpcGetChainInformation(CommandInfo ci);
        void RpcDeployContract(CommandInfo ci);
        void RpcBroadcastTx(CommandInfo ci);
        void RpcBroadcastWithRawTx(CommandInfo ci);
        string RpcGenerateTransactionRawTx(CommandInfo ci);
        string RpcGenerateTransactionRawTx(string from, string to, string methodName, IMessage inputParameter);
        void RpcBroadcastTxs(CommandInfo ci);
        void RpcGetCommands(CommandInfo ci);
        void RpcGetTxResult(CommandInfo ci);
        void RpcGetBlockHeight(CommandInfo ci);
        void RpcGetBlockInfo(CommandInfo ci);
        void RpcGetBlockByHeight(CommandInfo ci);
        void RpcGetBlockByHash(CommandInfo ci);
        void RpcGetMerklePath(CommandInfo ci);
        void RpcGetTransactionPoolStatus(CommandInfo ci);
        JObject RpcQueryView(string from, string to, string methodName, IMessage inputParameter);
        TResult RpcQueryView<TResult>(string from, string to, string methodName, IMessage inputParameter)
            where TResult : IMessage<TResult>, new();
        void RpcQueryViewInfo(CommandInfo ci);
        string GetPublicKeyFromAddress(string account, string password = "123");
        
        //net
        void NetGetPeers(CommandInfo ci);
        void NetAddPeer(CommandInfo ci);
        void NetRemovePeer(CommandInfo ci);
    }
}