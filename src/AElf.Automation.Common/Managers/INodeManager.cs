using System.Collections.Generic;
using AElf.Automation.Common.Helpers;
using AElfChain.SDK;
using AElfChain.SDK.Models;
using Google.Protobuf;

namespace AElf.Automation.Common.Managers
{
    public interface INodeManager
    {
        IApiService ApiService { get; set; }
        AccountManager AccountManager { get; }
        TransactionManager TransactionManager { get; }
        List<CommandInfo> CommandList { get; set; }
        string GetApiUrl();
        void UpdateApiUrl(string url);
        string GetChainId();
        string GetGenesisContractAddress();

        CommandInfo ExecuteCommand(CommandInfo ci);

        //account
        string NewAccount(string password = "");
        List<string> ListAccounts();
        bool UnlockAccount(string account, string password = "");

        //chain
        void GetChainInformation(CommandInfo ci);
        void DeployContract(CommandInfo ci);
        void BroadcastTx(CommandInfo ci);
        void BroadcastWithRawTx(CommandInfo ci);
        string GenerateTransactionRawTx(CommandInfo ci);
        string GenerateTransactionRawTx(string from, string to, string methodName, IMessage inputParameter);
        void BroadcastTxs(CommandInfo ci);

        TResult QueryView<TResult>(string from, string to, string methodName, IMessage inputParameter)
            where TResult : IMessage<TResult>, new();

        string GetPublicKeyFromAddress(string account, string password = "");

        //net
        List<PeerDto> NetGetPeers();
        bool NetAddPeer(string address);
        bool NetRemovePeer(string address);
    }
}