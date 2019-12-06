using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Acs0;
using AElf;
using AElfChain.Common.Contracts;
using AElfChain.Common.Helpers;
using AElf.Kernel;
using AElf.Types;
using AElfChain.Common.Utils;
using AElfChain.SDK;
using AElfChain.SDK.Models;
using Google.Protobuf;
using log4net;
using Volo.Abp.Threading;

namespace AElfChain.Common.Managers
{
    public class NodeManager : INodeManager
    {
        public NodeManager(string baseUrl, string keyPath = "")
        {
            _baseUrl = baseUrl;
            _keyStore = AElfKeyStore.GetKeyStore(keyPath);

            ApiService = AElfChainClient.GetClient(baseUrl);
            _chainId = GetChainId();
        }

        public string GetApiUrl()
        {
            return _baseUrl;
        }

        public void UpdateApiUrl(string url)
        {
            _baseUrl = url;
            ApiService = AElfChainClient.GetClient(url);
            _chainId = GetChainId();

            Logger.Info($"Request url updated to: {url}");
        }

        public string GetChainId()
        {
            if (_chainId != null)
                return _chainId;

            var chainStatus = AsyncHelper.RunSync(ApiService.GetChainStatusAsync);
            _chainId = chainStatus.ChainId;

            return _chainId;
        }

        public string GetGenesisContractAddress()
        {
            if (_genesisAddress != null) return _genesisAddress;

            var statusDto = AsyncHelper.RunSync(ApiService.GetChainStatusAsync);
            _genesisAddress = statusDto.GenesisContractAddress;

            return _genesisAddress;
        }

        private string CallTransaction(Transaction tx)
        {
            var rawTxString = TransactionManager.ConvertTransactionRawTxString(tx);
            return AsyncHelper.RunSync(() => ApiService.ExecuteTransactionAsync(rawTxString));
        }

        private TransactionManager GetTransactionManager()
        {
            if (_transactionManager != null) return _transactionManager;

            _transactionManager = new TransactionManager(_keyStore);
            return _transactionManager;
        }

        private AccountManager GetAccountManager()
        {
            if (_accountManager != null) return _accountManager;

            _accountManager = new AccountManager(_keyStore);
            return _accountManager;
        }

        #region Properties

        private string _baseUrl;
        private string _chainId;
        private readonly AElfKeyStore _keyStore;
        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        private string _genesisAddress;
        public string GenesisAddress => GetGenesisContractAddress();

        private AccountManager _accountManager;
        public AccountManager AccountManager => GetAccountManager();

        private TransactionManager _transactionManager;
        public TransactionManager TransactionManager => GetTransactionManager();
        public IApiService ApiService { get; set; }

        #endregion

        #region Account methods

        public string NewAccount(string password = "")
        {
            return AccountManager.NewAccount(password);
        }

        public string GetRandomAccount()
        {
            var accounts = AccountManager.ListAccount();
            var retry = 0;
            while (retry < 5)
            {
                retry++;
                var randomId = CommonHelper.GenerateRandomNumber(0, accounts.Count);
                var result = AccountManager.UnlockAccount(accounts[randomId]);
                if (!result) continue;

                return accounts[randomId];
            }

            throw new Exception("Cannot got account with default password.");
        }

        public string GetAccountPublicKey(string account, string password = "")
        {
            return AccountManager.GetPublicKey(account, password);
        }

        public List<string> ListAccounts()
        {
            return AccountManager.ListAccount();
        }

        public bool UnlockAccount(string account, string password = "")
        {
            return AccountManager.UnlockAccount(account, password);
        }

        #endregion

        #region Web request methods

        public string DeployContract(string from, string filename)
        {
            // Read sc bytes
            var contractReader = new SmartContractReader();
            var codeArray = contractReader.Read(filename);
            var input = new ContractDeploymentInput
            {
                Category = KernelConstants.CodeCoverageRunnerCategory,
                Code = ByteString.CopyFrom(codeArray)
            };

            var tx = TransactionManager.CreateTransaction(from, GenesisAddress,
                GenesisMethod.DeploySmartContract.ToString(), input.ToByteString());
            tx = tx.AddBlockReference(_baseUrl, _chainId);
            tx = TransactionManager.SignTransaction(tx);
            var rawTxString = TransactionManager.ConvertTransactionRawTxString(tx);
            var transactionOutput = AsyncHelper.RunSync(() => ApiService.SendTransactionAsync(rawTxString));

            return transactionOutput.TransactionId;
        }

        public string SendTransaction(string from, string to, string methodName, IMessage inputParameter)
        {
            var rawTransaction = GenerateRawTransaction(from, to, methodName, inputParameter);
            var transactionOutput = AsyncHelper.RunSync(() => ApiService.SendTransactionAsync(rawTransaction));

            return transactionOutput.TransactionId;
        }
        
        public string SendTransaction(string from, string to, string methodName, IMessage inputParameter, out bool existed)
        {
            var rawTransaction = GenerateRawTransaction(from, to, methodName, inputParameter);
            //check whether tx exist or not
            var genTxId = TransactionUtil.CalculateTxId(rawTransaction);
            var transactionResult = ApiService.GetTransactionResultAsync(genTxId).Result;
            if (transactionResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.NotExisted)
            {
                Logger.Warn("Found duplicate transaction.");
                existed = true;
                return transactionResult.TransactionId;
            }

            existed = false;
            var transactionOutput = AsyncHelper.RunSync(() => ApiService.SendTransactionAsync(rawTransaction));
            return transactionOutput.TransactionId;
        }

        public string SendTransaction(string rawTransaction)
        {
            var transactionOutput = AsyncHelper.RunSync(() => ApiService.SendTransactionAsync(rawTransaction));

            return transactionOutput.TransactionId;
        }

        public List<string> SendTransactions(string rawTransactions)
        {
            var transactions = AsyncHelper.RunSync(() => ApiService.SendTransactionsAsync(rawTransactions));

            return transactions.ToList();
        }

        public string GenerateRawTransaction(string from, string to, string methodName, IMessage inputParameter)
        {
            var tr = new Transaction
            {
                From = from.ConvertAddress(),
                To = to.ConvertAddress(),
                MethodName = methodName
            };

            if (tr.MethodName == null)
            {
                Logger.Error("Method not found.");
                return string.Empty;
            }

            tr.Params = inputParameter == null ? ByteString.Empty : inputParameter.ToByteString();
            tr = tr.AddBlockReference(_baseUrl, _chainId);

            TransactionManager.SignTransaction(tr);

            return tr.ToByteArray().ToHex();
        }

        public TransactionResultDto CheckTransactionResult(string txId, int maxTimes = -1)
        {
            if (maxTimes == -1) maxTimes = 600;

            var checkTimes = 1;
            var stopwatch = Stopwatch.StartNew();
            while (checkTimes <= maxTimes)
            {
                var transactionResult = AsyncHelper.RunSync(() => ApiService.GetTransactionResultAsync(txId));
                var status = transactionResult.Status.ConvertTransactionResultStatus();
                switch (status)
                {
                    case TransactionResultStatus.Mined:
                        Logger.Info(
                            $"Transaction {txId} Method:{transactionResult.Transaction.MethodName}, Status: {status}-[{transactionResult.TransactionFee?.GetTransactionFeeInfo()}]",
                            true);
                        return transactionResult;
                    case TransactionResultStatus.Failed:
                    {
                        var message =
                            $"Transaction {txId} status: {status}-[{transactionResult.TransactionFee?.GetTransactionFeeInfo()}]";
                        message +=
                            $"\r\nMethodName: {transactionResult.Transaction.MethodName}, Parameter: {transactionResult.Transaction.Params}";
                        message += $"\r\nError Message: {transactionResult.Error}";
                        Logger.Error(message, true);
                        return transactionResult;
                    }
                    case TransactionResultStatus.Pending:
                        checkTimes++;
                        break;
                    case TransactionResultStatus.NotExisted:
                        checkTimes += 10;
                        break;
                    case TransactionResultStatus.Unexecutable:
                        checkTimes += 20;
                        break;
                }

                Console.Write(
                    $"\rTransaction {txId} status: {status}, time using: {CommonHelper.ConvertMileSeconds(stopwatch.ElapsedMilliseconds)}");
                Thread.Sleep(500);
            }

            Console.Write("\r\n");
            throw new TimeoutException("Transaction execution status cannot be 'Mined' after long time.");
        }

        public void CheckTransactionListResult(List<string> transactionIds)
        {
            var transactionQueue = new ConcurrentQueue<string>();
            transactionIds.ForEach(transactionQueue.Enqueue);
            var stopwatch = Stopwatch.StartNew();
            while (transactionQueue.TryDequeue(out var transactionId))
            {
                var id = transactionId;
                var transactionResult = AsyncHelper.RunSync(() => ApiService.GetTransactionResultAsync(id));
                var status = transactionResult.Status.ConvertTransactionResultStatus();
                switch (status)
                {
                    case TransactionResultStatus.Pending:
                    case TransactionResultStatus.NotExisted:
                    case TransactionResultStatus.Unexecutable:
                        Console.Write(
                            $"\r[Processing]: TransactionId={id}, Status: {status}, using time:{CommonHelper.ConvertMileSeconds(stopwatch.ElapsedMilliseconds)}");
                        transactionQueue.Enqueue(id);
                        Thread.Sleep(500);
                        break;
                    case TransactionResultStatus.Mined:
                        Logger.Info($"TransactionId: {id}, Status: {status}", true);
                        break;
                    case TransactionResultStatus.Failed:
                        Logger.Error($"TransactionId: {id}, Status: {status}, Error: {transactionResult.Error}", true);
                        break;
                }
            }
            stopwatch.Stop();
        }

        public T QueryView<T>(string from, string to, string methodName, IMessage inputParameter)
            where T : IMessage<T>, new()
        {
            var transaction = new Transaction
            {
                From = from.ConvertAddress(),
                To = to.ConvertAddress(),
                MethodName = methodName,
                Params = inputParameter == null ? ByteString.Empty : inputParameter.ToByteString()
            };
            transaction = TransactionManager.SignTransaction(transaction);

            var resp = CallTransaction(transaction);

            //deserialize response
            if (resp == null)
            {
                Logger.Error("ExecuteTransaction response is null.");
                return default;
            }

            var byteArray = ByteArrayHelper.HexStringToByteArray(resp);
            var messageParser = new MessageParser<T>(() => new T());

            return messageParser.ParseFrom(byteArray);
        }

        public ByteString QueryView(string from, string to, string methodName, IMessage inputParameter)
        {
            var transaction = new Transaction
            {
                From = from.ConvertAddress(),
                To = to.ConvertAddress(),
                MethodName = methodName,
                Params = inputParameter == null ? ByteString.Empty : inputParameter.ToByteString()
            };
            transaction = TransactionManager.SignTransaction(transaction);

            var resp = CallTransaction(transaction);

            //deserialize response
            if (resp == null)
            {
                Logger.Error("ExecuteTransaction response is null.");
                return default;
            }

            var byteArray = ByteArrayHelper.HexStringToByteArray(resp);

            return ByteString.CopyFrom(byteArray);
        }

        //Net Api
        public List<PeerDto> NetGetPeers()
        {
            return AsyncHelper.RunSync(ApiService.GetPeersAsync);
        }

        public bool NetAddPeer(string address)
        {
            return AsyncHelper.RunSync(() => ApiService.AddPeerAsync(address));
        }

        public bool NetRemovePeer(string address)
        {
            return AsyncHelper.RunSync(() => ApiService.RemovePeerAsync(address));
        }

        public NetworkInfoOutput NetworkInfo()
        {
            return AsyncHelper.RunSync(ApiService.NetworkInfo);
        }

        #endregion
    }
}