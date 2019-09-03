using System.Collections.Generic;
using AElf.Automation.Common.OptionManagers;
using AElfChain.SDK;
using Google.Protobuf;
using Newtonsoft.Json.Linq;

namespace AElf.Automation.Common.Helpers
{
    public interface IApiHelper
    {
        string GetApiUrl();
        void UpdateApiUrl(string url);
        IApiService ApiService { get; set; }
        AccountManager AccountManager { get; }
        TransactionManager TransactionManager { get; }
        List<CommandInfo> CommandList { get; set; }
        string GetGenesisContractAddress();

        CommandInfo ExecuteCommand(CommandInfo ci);

        //account
        CommandInfo NewAccount(CommandInfo ci);
        CommandInfo ListAccounts();
        CommandInfo UnlockAccount(CommandInfo ci);

        //chain
        void GetChainInformation(CommandInfo ci);
        void DeployContract(CommandInfo ci);
        void BroadcastTx(CommandInfo ci);
        void BroadcastWithRawTx(CommandInfo ci);
        string GenerateTransactionRawTx(CommandInfo ci);
        string GenerateTransactionRawTx(string from, string to, string methodName, IMessage inputParameter);
        void BroadcastTxs(CommandInfo ci);
        void GetTransactionResult(CommandInfo ci);
        void GetBlockHeight(CommandInfo ci);
        void GetBlockByHeight(CommandInfo ci);
        void GetBlockByHash(CommandInfo ci);
        void GetTransactionPoolStatus(CommandInfo ci);
        JObject QueryView(string from, string to, string methodName, IMessage inputParameter);

        TResult QueryView<TResult>(string from, string to, string methodName, IMessage inputParameter)
            where TResult : IMessage<TResult>, new();

        void QueryViewInfo(CommandInfo ci);

        string GetPublicKeyFromAddress(string account, string password = "");

        //net
        void NetGetPeers(CommandInfo ci);
        void NetAddPeer(CommandInfo ci);
        void NetRemovePeer(CommandInfo ci);
    }
}