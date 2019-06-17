using System.Collections.Concurrent;
using System.Threading;
using Newtonsoft.Json.Linq;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi.Dto;
using AElf.Types;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.Common.Contracts
{
    public class BaseContract<T>
    {
        #region Priority

        public IApiHelper ApiHelper { get; set; }
        public string FileName { get; set; }
        public string CallAddress { get; set; }
        public Address CallAccount { get; set; }
        public string ContractAddress { get; set; }
        
        public static int Timeout { get; set; }
        private ConcurrentQueue<string> TxResultList { get; set; }
        protected static readonly ILogHelper Logger = LogHelper.GetLogHelper();

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
            CallAccount = Address.Parse(callAddress);
            TxResultList = new ConcurrentQueue<string>();

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
            TxResultList = new ConcurrentQueue<string>();
        }

        private BaseContract()
        {
        }

        public BaseContract<T> GetNewTester(string account, string password = "123")
        {
            return GetNewTester(ApiHelper, account, password);
        }

        public BaseContract<T> GetNewTester(IApiHelper apiHelper, string account, string password = "123")
        {
            UnlockAccount(account);
            
            var contract = new BaseContract<T>
            {
                ApiHelper = apiHelper,
                ContractAddress = ContractAddress,
                
                CallAccount = Address.Parse(account),
                CallAddress = account,
                
                TxResultList = new ConcurrentQueue<string>()
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
            string rawTx = GenerateBroadcastRawTx(method, inputParameter);

            var txId = ExecuteMethodWithTxId(rawTx);
            Logger.WriteInfo($"Transaction method: {method}, TxId: {txId}");
            TxResultList.Enqueue(txId);

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
        public CommandInfo ExecuteMethodWithResult(string method, IMessage inputParameter)
        {
            string rawTx = GenerateBroadcastRawTx(method, inputParameter);

            var txId = ExecuteMethodWithTxId(rawTx);
            Logger.WriteInfo($"Transaction method: {method}, TxId: {txId}");

            //Check result
            return CheckTransactionResult(txId);
        }
        
        /// <summary>
        /// 执行交易，等待执行结果后返回
        /// </summary>
        /// <param name="method"></param>
        /// <param name="inputParameter"></param>
        /// <returns></returns>
        public CommandInfo ExecuteMethodWithResult(T method, IMessage inputParameter)
        {
            return ExecuteMethodWithResult(method.ToString(), inputParameter);
        }

        /// <summary>
        /// 获取执交易行结果是否成功
        /// </summary>
        /// <param name="txId"></param>
        /// <param name="ci"></param>
        /// <returns></returns>
        public bool GetTransactionResult(string txId, out CommandInfo ci)
        {
            ci = new CommandInfo(ApiMethods.GetTransactionResult) {Parameter = txId};
            ApiHelper.ExecuteCommand(ci);

            if (ci.Result)
            {
                var transactionResult = ci.InfoMsg as TransactionResultDto;
                Logger.WriteInfo($"Transaction: {txId}, Status: {transactionResult?.Status}");

                return transactionResult?.Status == "Mined";
            }

            Logger.WriteError(ci.GetErrorMessage());
            return false;
        }

        /// <summary>
        /// 检查交易执行结果
        /// </summary>
        /// <param name="txId"></param>
        /// <param name="maxTimes"></param>
        /// <returns></returns>
        public CommandInfo CheckTransactionResult(string txId, int maxTimes = -1)
        {
            if (maxTimes == -1)
            {
                maxTimes = Timeout == 0 ? 600 : Timeout;
            }
            
            CommandInfo ci = null;
            var checkTimes = 1;
            while (checkTimes <= maxTimes)
            {
                ci = new CommandInfo(ApiMethods.GetTransactionResult) {Parameter = txId};
                ApiHelper.GetTransactionResult(ci);
                if (ci.Result)
                {
                    var transactionResult = ci.InfoMsg as TransactionResultDto;
                    switch (transactionResult?.Status)
                    {
                        case "Mined":
                        case "NotExisted":
                            Logger.WriteInfo($"Transaction {txId} status: {transactionResult.Status}");
                            return ci;
                        case "Failed":
                        {
                            var message = $"Transaction {txId} status: {transactionResult.Status}";
                            message += $"\r\nMethodName: {transactionResult.Transaction.MethodName}, Parameter: {transactionResult.Transaction.Params}";
                            message += $"\r\nError Message: {transactionResult.Error}";
                            Logger.WriteError(message);
                            return ci;
                        }
                    }
                }

                checkTimes++;
                Thread.Sleep(500);
            }

            if (ci != null)
            {
                var result = ci.InfoMsg as TransactionResultDto;
                Logger.WriteError(result?.Error);
            }

            Assert.IsTrue(false, "Transaction execute status cannot be 'Mined' after 5 minutes.");

            return ci;
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
            CallAccount = Address.Parse(account);

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
                var result = TxResultList.TryDequeue(out var txId);
                if (!result)
                    break;
                var ci = new CommandInfo(ApiMethods.GetTransactionResult) {Parameter = txId};
                ApiHelper.GetTransactionResult(ci);
                if (ci.Result)
                {
                    var transactionResult = ci.InfoMsg as TransactionResultDto;
                    switch (transactionResult?.Status)
                    {
                        case "Mined":
                            continue;
                        case "Failed":
                        case "NotExisted":
                        {
                            var message = $"Transaction {txId} status: {transactionResult.Status}\r\n";
                            message += $"{transactionResult.Error}";
                            Logger.WriteError(message);
                            continue;
                        }
                        default:
                            TxResultList.Enqueue(txId);
                            break;
                    }
                }

                if (queueLength == TxResultList.Count)
                {
                    queueSameTimes++;
                    Thread.Sleep(1000);
                }
                else
                    queueSameTimes = 0;

                queueLength = TxResultList.Count;
                if (queueSameTimes == 300)
                    Assert.IsTrue(false, "Transaction result check failed due to pending results in 5 minutes.");
            }
        }

        /// <summary>
        /// 调用合约View方法
        /// </summary>
        /// <param name="method"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        public JObject CallViewMethod(string method, IMessage input)
        {
            return ApiHelper.QueryView(CallAddress, ContractAddress, method, input);
        }

        /// <summary>
        /// 调用合约View方法
        /// </summary>
        /// <param name="method"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        public JObject CallViewMethod(T method, IMessage input)
        {
            return CallViewMethod(method.ToString(), input);
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
                    Logger.WriteInfo($"Transaction: DeploySmartContract, TxId: {txId}");

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
            var ci = CheckTransactionResult(txId);

            if (!ci.Result) return false;
            var transactionResult = ci.InfoMsg as TransactionResultDto;
            Logger.WriteInfo($"Transaction: {txId}, Status: {transactionResult?.Status}");
            if (transactionResult?.Status != "Mined") return false;
            contractAddress = transactionResult.ReadableReturnValue.Replace("\"", "");
            ContractAddress = contractAddress;
            Logger.WriteInfo($"Get contract address: TxId: {txId}, Address: {contractAddress}");
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