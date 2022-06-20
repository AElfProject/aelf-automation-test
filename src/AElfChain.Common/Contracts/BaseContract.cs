using System;
using System.Collections.Concurrent;
using System.Threading;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using log4net;
using Newtonsoft.Json;
using Volo.Abp.Threading;

namespace AElfChain.Common.Contracts
{
    public class BaseContract<T>
    {
        /// <summary>
        ///     deploy new contract
        /// </summary>
        /// <param name="nodeManager"></param>
        /// <param name="fileName"></param>
        /// <param name="callAddress"></param>
        protected BaseContract(INodeManager nodeManager, string fileName, string callAddress)
        {
            NodeManager = nodeManager;
            FileName = fileName;
            CallAddress = callAddress;
            
            DeployContract(callAddress);
        }

        /// <summary>
        ///     Initialize existed contract
        /// </summary>
        /// <param name="nodeManager"></param>
        /// <param name="contractAddress"></param>
        protected BaseContract(INodeManager nodeManager, string contractAddress)
        {
            NodeManager = nodeManager;
            ContractAddress = contractAddress;
        }

        private BaseContract()
        {
        }

        /// <summary>
        ///     get contract stub
        /// </summary>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <typeparam name="TStub"></typeparam>
        /// <returns></returns>
        public TStub GetTestStub<TStub>(string account, string password = "")
            where TStub : ContractStubBase, new()
        {
            var stub = new ContractTesterFactory(NodeManager);
            var testStub =
                stub.Create<TStub>(Contract, account, password);

            return testStub;
        }

        public BaseContract<T> GetNewTester(string account, string password = "")
        {
            return GetNewTester(NodeManager, account, password);
        }

        private BaseContract<T> GetNewTester(INodeManager nodeManager, string account, string password = "")
        {
            SetAccount(account, password);
            var newTester = new BaseContract<T>
            {
                NodeManager = nodeManager,
                ContractAddress = ContractAddress,
                CallAddress = account
            };

            return newTester;
        }

        /// <summary>
        ///     execute tx and get transaction id
        /// </summary>
        /// <param name="method"></param>
        /// <param name="inputParameter"></param>
        /// <returns></returns>
        public string ExecuteMethodWithTxId(string method, IMessage inputParameter)
        {
            var rawTx = GenerateBroadcastRawTx(method, inputParameter);

            var txId = NodeManager.SendTransaction(rawTx);
            Logger.Info($"Transaction method: {method}, TxId: {txId}");
            _txResultList.Enqueue(txId);

            return txId;
        }

        /// <summary>
        ///     send tx and get transaction id
        /// </summary>
        /// <param name="method"></param>
        /// <param name="inputParameter"></param>
        /// <returns></returns>
        public string ExecuteMethodWithTxId(T method, IMessage inputParameter)
        {
            return ExecuteMethodWithTxId(method.ToString(), inputParameter);
        }

        /// <summary>
        ///     execution tx and wait result response
        /// </summary>
        /// <param name="method"></param>
        /// <param name="inputParameter"></param>
        /// <returns></returns>
        public TransactionResultDto ExecuteMethodWithResult(string method, IMessage inputParameter)
        {
            var rawTx = GenerateBroadcastRawTx(method, inputParameter);
            var txId = NodeManager.SendTransaction(rawTx);
            Logger.Info($"Transaction method: {method}, TxId: {txId}");

            //Check result
            Thread.Sleep(100); //in case of 'NotExisted' issue
            return NodeManager.CheckTransactionResult(txId);
        }

        /// <summary>
        ///     check tx whether exist or not before execution
        /// </summary>
        /// <param name="method"></param>
        /// <param name="inputParameter"></param>
        /// <param name="existed"></param>
        /// <returns></returns>
        public TransactionResultDto ExecuteMethodWithResult(string method, IMessage inputParameter, out bool existed)
        {
            var rawTx = GenerateBroadcastRawTx(method, inputParameter);
            //check whether tx exist or not
            var genTxId = TransactionUtil.CalculateTxId(rawTx);
            var transactionResult = AsyncHelper.RunSync(() => ApiClient.GetTransactionResultAsync(genTxId));
            if (transactionResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.NotExisted)
            {
                Logger.Warn("Found duplicate transaction.");
                existed = true;
                return transactionResult;
            }

            existed = false;
            var txId = NodeManager.SendTransaction(rawTx);
            Logger.Info($"Transaction method: {method}, TxId: {txId}");

            //Check result
            Thread.Sleep(100); //in case of 'NotExisted' issue
            return NodeManager.CheckTransactionResult(txId);
        }

        /// <summary>
        ///     execution tx and wait execution result response
        /// </summary>
        /// <param name="method">tx method</param>
        /// <param name="inputParameter">tx input parameter</param>
        /// <returns></returns>
        public TransactionResultDto ExecuteMethodWithResult(T method, IMessage inputParameter)
        {
            return ExecuteMethodWithResult(method.ToString(), inputParameter);
        }

        /// <summary>
        ///     execution tx and check if exist return result
        /// </summary>
        /// <param name="method"></param>
        /// <param name="inputParameter"></param>
        /// <param name="existed"></param>
        /// <returns></returns>
        public TransactionResultDto ExecuteMethodWithResult(T method, IMessage inputParameter, out bool existed)
        {
            return ExecuteMethodWithResult(method.ToString(), inputParameter, out existed);
        }

        /// <summary>
        ///     switch contract execution owner
        /// </summary>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public bool SetAccount(string account, string password = "")
        {
            CallAddress = account;

            return NodeManager.UnlockAccount(account, password);
        }

        /// <summary>
        ///     check all txs results
        /// </summary>
        public void CheckTransactionResultList()
        {
            var queueLength = 0;
            var queueSameTimes = 0;

            while (true)
            {
                var result = _txResultList.TryDequeue(out var txId);
                if (!result) break;
                var transactionResult = AsyncHelper.RunSync(() => ApiClient.GetTransactionResultAsync(txId));
                var status = transactionResult.Status.ConvertTransactionResultStatus();
                var fee = transactionResult.GetDefaultTransactionFee();
                switch (status)
                {
                    case TransactionResultStatus.Mined:
                        Logger.Info(
                            $"TransactionId: {transactionResult.TransactionId}, Method: {transactionResult.Transaction.MethodName}, Status: {transactionResult.Status}, Fee: {fee}");
                        continue;
                    case TransactionResultStatus.Failed:
                    {
                        Logger.Error($"TransactionId: {transactionResult.TransactionId}, Method: {transactionResult.Transaction.MethodName}, Status: {transactionResult.Status}, Fee: {fee}");
                        Logger.Error(JsonConvert.SerializeObject(transactionResult, Formatting.Indented));
                        continue;
                    }
                    case TransactionResultStatus.Conflict:
                    {
                        Logger.Error($"TransactionId: {transactionResult.TransactionId}, Method: {transactionResult.Transaction.MethodName}, Status: {transactionResult.Status}, Fee: {fee}");
                        Logger.Error(JsonConvert.SerializeObject(transactionResult, Formatting.Indented));
                        continue;
                    }
                    default:
                        _txResultList.Enqueue(txId);
                        break;
                }

                if (queueLength == _txResultList.Count)
                {
                    queueSameTimes++;
                    Thread.Sleep(2000);
                }
                else
                {
                    queueSameTimes = 0;
                }

                queueLength = _txResultList.Count;
                if (queueSameTimes == 300)
                    throw new TimeoutException("Transaction result check failed due to pending results long time.");
            }
        }

        /// <summary>
        ///     call contract view method
        /// </summary>
        /// <param name="method"></param>
        /// <param name="input"></param>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public TResult CallViewMethod<TResult>(string method, IMessage input) where TResult : IMessage<TResult>, new()
        {
            return NodeManager.QueryView<TResult>(CallAddress, ContractAddress, method, input);
        }

        /// <summary>
        ///     call view method
        /// </summary>
        /// <param name="method"></param>
        /// <param name="input"></param>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public TResult CallViewMethod<TResult>(T method, IMessage input) where TResult : IMessage<TResult>, new()
        {
            return CallViewMethod<TResult>(method.ToString(), input);
        }

        #region Priority

        public INodeManager NodeManager { get; set; }
        public AElfClient ApiClient => NodeManager.ApiClient;
        public string FileName { get; set; }
        public string CallAddress { get; set; }
        public Address CallAccount => CallAddress.ConvertAddress();
        public string ContractAddress { get; set; }
        public Address Contract => ContractAddress.ConvertAddress();

        public static ILog Logger = Log4NetHelper.GetLogger();

        private readonly ConcurrentQueue<string> _txResultList = new ConcurrentQueue<string>();

        #endregion

        #region Private Methods

        private void DeployContract(string account)
        {
            Logger.Info("Deploy contract with authority mode.");
            var authority = new AuthorityManager(NodeManager, account);
            var contractAddress = authority.DeployContractWithAuthority(account, FileName);
            ContractAddress = contractAddress.ToBase58();
        }

        private string GenerateBroadcastRawTx(string method, IMessage inputParameter)
        {
            return NodeManager.GenerateRawTransaction(CallAddress, ContractAddress, method, inputParameter);
        }

        #endregion Methods
    }
}