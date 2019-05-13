using System.Collections.Concurrent;
using System.Threading;
using Newtonsoft.Json.Linq;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi.Dto;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AElf.Automation.Common.Contracts
{
    public class BaseContract<T>
    {
        #region Priority

        public IApiHelper ApiHelper { get; set; }
        private string FileName { get; set; }
        public string CallAddress { get; set; }
        public Address CallAccount { get; set; }
        public string ContractAddress { get; set; }
        private ConcurrentQueue<string> TxResultList { get; set; }
        private readonly ILogHelper _logger = LogHelper.GetLogHelper();

        #endregion

        /// <summary>
        /// 部署新合约
        /// </summary>
        /// <param name="apiHelper"></param>
        /// <param name="fileName"></param>
        /// <param name="callAddress"></param>
        public BaseContract(IApiHelper apiHelper, string fileName, string callAddress)
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
        public BaseContract(IApiHelper apiHelper, string contractAddress)
        {
            ApiHelper = apiHelper;
            ContractAddress = contractAddress;
            TxResultList = new ConcurrentQueue<string>();
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
            _logger.WriteInfo($"Transaction method: {method}, TxId: {txId}");
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
            _logger.WriteInfo($"Transaction method: {method}, TxId: {txId}");
            _logger.WriteInfo($"Transaction rawTx: {rawTx}");

            //Check result
            return CheckTransactionResult(txId, 30);
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
            ci = new CommandInfo(ApiMethods.GetTransactionResult);
            ci.Parameter = txId;
            ApiHelper.ExecuteCommand(ci);

            if (ci.Result)
            {
                var transactionResult = ci.InfoMsg as TransactionResultDto;
                _logger.WriteInfo($"Transaction: {txId}, Status: {transactionResult?.Status}");

                return transactionResult?.Status == "Mined";
            }

            _logger.WriteError(ci.GetErrorMessage());
            return false;
        }

        /// <summary>
        /// 检查交易执行结果
        /// </summary>
        /// <param name="txId"></param>
        /// <param name="maxTimes"></param>
        /// <returns></returns>
        public CommandInfo CheckTransactionResult(string txId, int maxTimes = 60)
        {
            CommandInfo ci = null;
            int checkTimes = 1;
            while (checkTimes <= maxTimes)
            {
                ci = new CommandInfo(ApiMethods.GetTransactionResult);
                ci.Parameter = txId;
                ApiHelper.GetTxResult(ci);
                if (ci.Result)
                {
                    var transactionResult = ci.InfoMsg as TransactionResultDto;
                    if (transactionResult?.Status == "Mined")
                    {
                        _logger.WriteInfo($"Transaction status: {transactionResult?.Status}");
                        return ci;
                    }

                    if (transactionResult?.Status == "Failed")
                    {
                        _logger.WriteInfo($"Transaction status: {transactionResult?.Status}");
                        _logger.WriteError(transactionResult?.Error);
                        return ci;
                    }
                }

                checkTimes++;
                Thread.Sleep(1000);
            }

            var result = ci.InfoMsg as TransactionResultDto;
            _logger.WriteError(result?.Error);
            Assert.IsTrue(false, "Transaction execute status cannot be 'Mined' after one minutes.");

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
            int queueLength = 0;
            int queueSameTimes = 0;

            while (true)
            {
                bool result = TxResultList.TryDequeue(out var txId);
                if (!result)
                    break;
                var ci = new CommandInfo(ApiMethods.GetTransactionResult) {Parameter = txId};
                ApiHelper.GetTxResult(ci);
                if (ci.Result)
                {
                    var transactionResult = ci.InfoMsg as TransactionResultDto;
                    if (transactionResult?.Status == "Mined")
                        continue;
                    if (transactionResult?.Status == "Failed" || transactionResult?.Status == "NotExisted")
                    {
                        _logger.WriteInfo($"Transaction status: {transactionResult.Status}");
                        _logger.WriteError(ci.JsonInfo.ToString());
                        continue;
                    }

                    TxResultList.Enqueue(txId);
                }

                if (queueLength == TxResultList.Count)
                {
                    queueSameTimes++;
                    Thread.Sleep(3000);
                }
                else
                    queueSameTimes = 0;

                queueLength = TxResultList.Count;
                if (queueSameTimes == 10)
                    Assert.IsTrue(false, "Transaction result check failed due to pending results.");
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
                if (ci.InfoMsg is BroadcastTransactionOutput transactionOutput)
                {
                    var txId = transactionOutput.TransactionId;
                    _logger.WriteInfo($"Transaction: DeploySmartContract, TxId: {txId}");

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
            _logger.WriteInfo($"Transaction: {txId}, Status: {transactionResult?.Status}");
            if (transactionResult?.Status != "Mined") return false;
            contractAddress = transactionResult.ReadableReturnValue.Replace("\"", "");
            ContractAddress = contractAddress;
            _logger.WriteInfo($"Get contract address: TxId: {txId}, Address: {contractAddress}");
            return true;
        }

        private string ExecuteMethodWithTxId(string rawTx)
        {
            var ci = new CommandInfo(ApiMethods.BroadcastTransaction)
            {
                Parameter = rawTx
            };
            ApiHelper.BroadcastTx(ci);
            if (ci.Result)
            {
                var transactionOutput = ci.InfoMsg as BroadcastTransactionOutput;

                return transactionOutput?.TransactionId;
            }

            Assert.IsTrue(ci.Result, $"Execute contract failed. Reason: {ci.GetErrorMessage()}");

            return string.Empty;
        }

        #endregion Methods
    }
}