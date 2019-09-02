using System;
using System.Collections.Concurrent;
using System.Threading;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.OptionManagers.Authority;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.SDK;
using AElfChain.SDK.Models;
using Google.Protobuf;
using log4net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volo.Abp.Threading;
using ApiMethods = AElf.Automation.Common.Helpers.ApiMethods;

namespace AElf.Automation.Common.Contracts
{
    public class BaseContract<T>
    {
        #region Priority

        public IApiHelper ApiHelper { get; set; }
        public IApiService ApiService => ApiHelper.ApiService;
        public string FileName { get; set; }
        public string CallAddress { get; set; }
        public Address CallAccount => AddressHelper.Base58StringToAddress(CallAddress);
        public string ContractAddress { get; set; }
        public Address Contract => AddressHelper.Base58StringToAddress(ContractAddress);

        public static int Timeout { get; set; }
        
        public static ILog Logger = Log4NetHelper.GetLogger();
        
        private readonly ConcurrentQueue<string> _txResultList = new ConcurrentQueue<string>();

        #endregion

        /// <summary>
        /// 部署新合约
        /// </summary>
        /// <param name="apiHelper"></param>
        /// <param name="fileName"></param>
        /// <param name="callAddress"></param>
        protected BaseContract(IApiHelper apiHelper, string fileName, string callAddress)
        {
            ApiHelper = apiHelper;
            FileName = fileName;
            CallAddress = callAddress;

            UnlockAccount(callAddress);
            DeployContract();
        }

        /// <summary>
        /// 使用已存在合约
        /// </summary>
        /// <param name="apiHelper"></param>
        /// <param name="contractAddress"></param>
        protected BaseContract(IApiHelper apiHelper, string contractAddress)
        {
            ApiHelper = apiHelper;
            ContractAddress = contractAddress;
        }

        private BaseContract()
        {
        }

        /// <summary>
        /// 获取合约Stub
        /// </summary>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <typeparam name="TStub"></typeparam>
        /// <returns></returns>
        public TStub GetTestStub<TStub>(string account, string password = "123")
            where TStub : ContractStubBase, new()
        {
            var stub = new ContractTesterFactory(ApiHelper);
            var testStub =
                stub.Create<TStub>(Contract, account, password);

            return testStub;
        }

        public BaseContract<T> GetNewTester(string account, string password = "123")
        {
            return GetNewTester(ApiHelper, account, password);
        }

        private BaseContract<T> GetNewTester(IApiHelper apiHelper, string account, string password = "123")
        {
            UnlockAccount(account, password);

            var contract = new BaseContract<T>
            {
                ApiHelper = apiHelper,
                ContractAddress = ContractAddress,
                CallAddress = account,
            };

            return contract;
        }

        /// <summary>
        /// 执行交易，返回TransactionId，不等待执行结果
        /// </summary>
        /// <param name="method"></param>
        /// <param name="inputParameter"></param>
        /// <returns></returns>
        public string ExecuteMethodWithTxId(string method, IMessage inputParameter)
        {
            var rawTx = GenerateBroadcastRawTx(method, inputParameter);

            var txId = ExecuteMethodWithTxId(rawTx);
            Logger.Info($"Transaction method: {method}, TxId: {txId}");
            _txResultList.Enqueue(txId);

            return txId;
        }

        /// <summary>
        /// 执行交易，返回TransactionId，不等待执行结果
        /// </summary>
        /// <param name="method"></param>
        /// <param name="inputParameter"></param>
        /// <returns></returns>
        public string ExecuteMethodWithTxId(T method, IMessage inputParameter)
        {
            return ExecuteMethodWithTxId(method.ToString(), inputParameter);
        }

        /// <summary>
        /// 执行交易，等待执行结果后返回
        /// </summary>
        /// <param name="method"></param>
        /// <param name="inputParameter"></param>
        /// <returns></returns>
        public TransactionResultDto ExecuteMethodWithResult(string method, IMessage inputParameter)
        {
            var rawTx = GenerateBroadcastRawTx(method, inputParameter);

            var txId = ExecuteMethodWithTxId(rawTx);
            Logger.Info($"Transaction method: {method}, TxId: {txId}");

            //Check result
            Thread.Sleep(100); //in case of 'NotExisted' issue
            return CheckTransactionResult(txId);
        }

        /// <summary>
        /// 执行交易，等待执行结果后返回
        /// </summary>
        /// <param name="method">交易方法</param>
        /// <param name="inputParameter">交易参数</param>
        /// <returns></returns>
        public TransactionResultDto ExecuteMethodWithResult(T method, IMessage inputParameter)
        {
            return ExecuteMethodWithResult(method.ToString(), inputParameter);
        }

        /// <summary>
        /// 获取执交易行结果是否成功
        /// </summary>
        /// <param name="txId"></param>
        /// <param name="transactionResult"></param>
        /// <returns></returns>
        public bool GetTransactionResult(string txId, out TransactionResultDto transactionResult)
        {
            transactionResult = AsyncHelper.RunSync(() => ApiService.GetTransactionResultAsync(txId));

            Logger.Info($"Transaction: {txId}, Status: {transactionResult.Status}");
            return transactionResult.Status.ConvertTransactionResultStatus() == TransactionResultStatus.Mined;
        }

        /// <summary>
        /// 检查交易执行结果
        /// </summary>
        /// <param name="txId"></param>
        /// <param name="maxTimes"></param>
        /// <returns></returns>
        public TransactionResultDto CheckTransactionResult(string txId, int maxTimes = -1)
        {
            if (maxTimes == -1)
            {
                maxTimes = Timeout == 0 ? 600 : Timeout;
            }

            var checkTimes = 1;
            while (checkTimes <= maxTimes)
            {
                var transactionResult = AsyncHelper.RunSync(() => ApiService.GetTransactionResultAsync(txId));
                var status = transactionResult.Status.ConvertTransactionResultStatus();
                switch (status)
                {
                    case TransactionResultStatus.Mined:
                        Logger.Info($"Transaction {txId} status: {transactionResult.Status}");
                        return transactionResult;
                    case TransactionResultStatus.NotExisted:
                        Logger.Error($"Transaction {txId} status: {transactionResult.Status}");
                        break;
                    case TransactionResultStatus.Failed:
                    {
                        var message = $"Transaction {txId} status: {transactionResult.Status}";
                        message +=
                            $"\r\nMethodName: {transactionResult.Transaction.MethodName}, Parameter: {transactionResult.Transaction.Params}";
                        message += $"\r\nError Message: {transactionResult.Error}";
                        Logger.Error(message);
                        return transactionResult;
                    }
                }

                checkTimes++;
                Thread.Sleep(500);
            }

            throw new Exception("Transaction execution status cannot be 'Mined' after five minutes.");
        }

        /// <summary>
        /// 切换执行用户
        /// </summary>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public bool SetAccount(string account, string password = "123")
        {
            CallAddress = account;
            //Unlock
            var uc = new CommandInfo(ApiMethods.AccountUnlock)
            {
                Parameter = $"{account} {password} notimeout"
            };
            uc = ApiHelper.UnlockAccount(uc);

            return uc.Result;
        }

        /// <summary>
        /// 检查所有执行合约结果
        /// </summary>
        public void CheckTransactionResultList()
        {
            var queueLength = 0;
            var queueSameTimes = 0;

            while (true)
            {
                var result = _txResultList.TryDequeue(out var txId);
                if (!result)
                    break;
                var ci = new CommandInfo(ApiMethods.GetTransactionResult) {Parameter = txId};
                ApiHelper.GetTransactionResult(ci);
                if (ci.Result)
                {
                    if (ci.InfoMsg is TransactionResultDto transactionResult)
                    {
                        var status = transactionResult.Status.ConvertTransactionResultStatus();
                        switch (status)
                        {
                            case TransactionResultStatus.Mined:
                                continue;
                            case TransactionResultStatus.Failed:
                            case TransactionResultStatus.Unexecutable:
                            {
                                var message = $"Transaction {txId} status: {transactionResult.Status}\r\n";
                                message += $"{transactionResult.Error}";
                                Logger.Error(message);
                                continue;
                            }
                            default:
                                _txResultList.Enqueue(txId);
                                break;
                        }
                    }
                }

                if (queueLength == _txResultList.Count)
                {
                    queueSameTimes++;
                    Thread.Sleep(1000);
                }
                else
                    queueSameTimes = 0;

                queueLength = _txResultList.Count;
                if (queueSameTimes == 300)
                    Assert.IsTrue(false, "Transaction result check failed due to pending results in 5 minutes.");
            }
        }

        /// <summary>
        /// 调用合约View方法
        /// </summary>
        /// <param name="method"></param>
        /// <param name="input"></param>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public TResult CallViewMethod<TResult>(string method, IMessage input) where TResult : IMessage<TResult>, new()
        {
            return ApiHelper.QueryView<TResult>(CallAddress, ContractAddress, method, input);
        }

        /// <summary>
        /// 调用合约View方法
        /// </summary>
        /// <param name="method"></param>
        /// <param name="input"></param>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public TResult CallViewMethod<TResult>(T method, IMessage input) where TResult : IMessage<TResult>, new()
        {
            return ApiHelper.QueryView<TResult>(CallAddress, ContractAddress, method.ToString(), input);
        }

        protected void UnlockAccount(string account, string password = "123")
        {
            var uc = new CommandInfo(ApiMethods.AccountUnlock)
            {
                Parameter = $"{account} {password} notimeout"
            };
            ApiHelper.UnlockAccount(uc);
        }

        #region Private Methods

        private void DeployContract()
        {
            var requireAuthority = NodeInfoHelper.Config.RequireAuthority;
            if (requireAuthority)
            {
                Logger.Info("Deploy contract with authority mode.");
                var authority = new AuthorityManager(ApiHelper, CallAddress);
                var contractAddress = authority.DeployContractWithAuthority(CallAddress, FileName);
                ContractAddress = contractAddress.GetFormatted();
                return;
            }

            Logger.Info("Deploy contract without authority mode.");
            var ci = new CommandInfo(ApiMethods.DeploySmartContract)
            {
                Parameter = $"{FileName} {CallAddress}"
            };
            ApiHelper.DeployContract(ci);
            if (ci.Result)
            {
                if (ci.InfoMsg is SendTransactionOutput transactionOutput)
                {
                    var txId = transactionOutput.TransactionId;
                    Logger.Info($"Transaction: DeploySmartContract, TxId: {txId}");

                    var result = GetContractAddress(txId, out _);
                    Assert.IsTrue(result, "Get contract address failed.");
                }
            }

            Assert.IsTrue(ci.Result, $"Deploy contract failed. Reason: {ci.GetErrorMessage()}");
        }

        private string GenerateBroadcastRawTx(string method, IMessage inputParameter)
        {
            return ApiHelper.GenerateTransactionRawTx(CallAddress, ContractAddress, method, inputParameter);
        }

        private bool GetContractAddress(string txId, out string contractAddress)
        {
            contractAddress = string.Empty;
            var transactionResult = CheckTransactionResult(txId);
            if (transactionResult?.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined)
                return false;

            contractAddress = transactionResult.ReadableReturnValue.Replace("\"", "");
            ContractAddress = contractAddress;
            Logger.Info($"Get contract address: TxId: {txId}, Address: {contractAddress}");
            return true;
        }

        private string ExecuteMethodWithTxId(string rawTx)
        {
            var ci = new CommandInfo(ApiMethods.SendTransaction)
            {
                Parameter = rawTx
            };
            ApiHelper.BroadcastWithRawTx(ci);
            if (ci.Result)
            {
                var transactionOutput = ci.InfoMsg as SendTransactionOutput;

                return transactionOutput?.TransactionId;
            }

            Assert.IsTrue(ci.Result, $"Execute contract failed. Reason: {ci.GetErrorMessage()}");

            return string.Empty;
        }

        #endregion Methods
    }
}