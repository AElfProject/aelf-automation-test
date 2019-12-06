using System.Collections.Generic;
using AElfChain.SDK;
using AElfChain.SDK.Models;
using Google.Protobuf;

namespace AElfChain.Common.Managers
{
    public interface INodeManager
    {
        IApiService ApiService { get; set; }
        AccountManager AccountManager { get; }
        TransactionManager TransactionManager { get; }
        string GetApiUrl();
        void UpdateApiUrl(string url);
        string GetChainId();
        string GetGenesisContractAddress();

        //account
        string NewAccount(string password = "");
        string GetRandomAccount();
        string GetAccountPublicKey(string account, string password = "");
        List<string> ListAccounts();
        bool UnlockAccount(string account, string password = "");

        //chain
        string DeployContract(string from, string filename);
        string SendTransaction(string from, string to, string methodName, IMessage inputParameter);
        string SendTransaction(string from, string to, string methodName, IMessage inputParameter, out bool existed);
        string SendTransaction(string rawTransaction);
        List<string> SendTransactions(string rawTransactions);
        string GenerateRawTransaction(string from, string to, string methodName, IMessage inputParameter);
        TransactionResultDto CheckTransactionResult(string txId, int maxTimes = -1);
        void CheckTransactionListResult(List<string> transactionIds);

        TResult QueryView<TResult>(string from, string to, string methodName, IMessage inputParameter)
            where TResult : IMessage<TResult>, new();

        ByteString QueryView(string from, string to, string methodName, IMessage inputParameter);

        //net
        List<PeerDto> NetGetPeers();
        bool NetAddPeer(string address);
        bool NetRemovePeer(string address);
        NetworkInfoOutput NetworkInfo();
    }
}