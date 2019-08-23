using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf;
using AElf.Automation.Common.Helpers;
using AElf.Types;
using AElfChain.SDK;
using AElfChain.SDK.Models;
using Google.Protobuf;
using log4net;
using Newtonsoft.Json;
using Volo.Abp.Threading;

namespace AElfChain.AccountService
{
    public class TransactionManager : ITransactionManager
    {
        private readonly IAccountManager _accountManager;
        private readonly IApiService _apiService;

        private string _chainId;
        private int _checkTimeout;
        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        public static ITransactionManager GetTransactionManager(string serviceUrl, string accountPath = "")
        {
            var accountManager = AccountManager.GetAccountManager(accountPath);
            var apiService = AElfChainClient.GetClient(serviceUrl);
            
            return new TransactionManager(apiService, accountManager);
        }
        
        private TransactionManager(IApiService apiService, IAccountManager accountManager)
        {
            _apiService = apiService;
            _accountManager = accountManager;
            _checkTimeout = 60;

            _chainId = AsyncHelper.RunSync(_apiService.GetChainStatusAsync).ChainId;
        }
        
        public async Task<Transaction> CreateTransaction(string from, string to, string method, ByteString input)
        {
            try
            {
                var transaction = new Transaction
                {
                    From = AddressHelper.Base58StringToAddress(from),
                    To = AddressHelper.Base58StringToAddress(to),
                    MethodName = method,
                    Params = input ?? ByteString.Empty
                };

                transaction = await AddBlockReference(transaction);
                transaction = await _accountManager.SignTransactionAsync(transaction);

                return transaction;
            }
            catch (Exception e)
            {
                Logger.Error($"Invalid transaction data: {e.Message}");
                return null;
            }
        }

        public async Task<string> SendTransactionWithIdAsync(Transaction transaction)
        {
            var transactionOutput = await _apiService.SendTransactionAsync(transaction.ToByteArray().ToHex());
            Logger.Info($"Transaction method: {transaction.MethodName}, TxId: {transactionOutput.TransactionId}");
            return transactionOutput.TransactionId;
        }

        public async Task<List<string>> SendBatchTransactionWithIdAsync(List<Transaction> transactions)
        {
            var rawTransactions = string.Join(",", transactions.Select(o => o.ToByteArray().ToHex()));
            var txIds = await _apiService.SendTransactionsAsync(rawTransactions);

            return txIds.ToList();
        }

        public async Task<TransactionResultDto> SendTransactionWithResultAsync(Transaction transaction)
        {
            var transactionId = await SendTransactionWithIdAsync(transaction);

            TransactionResultDto transactionResult = null;
            using (var cts = new CancellationTokenSource())
            {
                cts.CancelAfter(_checkTimeout * 1000);
                var completed = false;
                while (!completed)
                {
                    if (cts.IsCancellationRequested)
                        break;
                    await Task.Delay(500, cts.Token);

                    transactionResult = await QueryTransactionResultAsync(transactionId);
                    var transactionStatus = transactionResult.Status.ConvertTransactionResultStatus();
                    switch (transactionStatus)
                    {
                        case TransactionResultStatus.Mined:
                            Logger.Info($"Transaction: {transactionId}, Status: {transactionStatus}");
                            completed = true;
                            break;
                        case TransactionResultStatus.Failed:
                        case TransactionResultStatus.Unexecutable:
                            Logger.Error($"Failed transaction: {JsonConvert.SerializeObject(transactionResult)}");
                            completed = true;
                            break;
                        default:
                            $"Transaction: {transactionId}, Status: {transactionStatus}".WriteWarningLine();
                            break;    
                    }
                }
            }

            return transactionResult;
        }

        public async Task<TransactionResultDto> QueryTransactionResultAsync(string transactionId)
        {
            var transactionResult =  await _apiService.GetTransactionResultAsync(transactionId);
            return transactionResult;
        }

        public async Task<List<TransactionResultDto>> QueryTransactionsResultAsync(List<string> transactionIds)
        {
            var transactionResults = new List<TransactionResultDto>();
            var transactionIdQueue = new ConcurrentQueue<string>(transactionIds);
            while (transactionIdQueue.TryPeek(out var txId))
            {
                var transactionResult = await QueryTransactionResultAsync(txId);
                var transactionStatus = transactionResult.Status.ConvertTransactionResultStatus();
                switch (transactionStatus)
                {
                    case TransactionResultStatus.Mined:
                        Logger.Info($"Transaction: {txId}, Status: {transactionStatus}");
                        transactionResults.Add(transactionResult);
                        break;
                    case TransactionResultStatus.Failed:
                    case TransactionResultStatus.Unexecutable:
                        Logger.Error($"Failed transaction: {JsonConvert.SerializeObject(transactionResult)}");
                        transactionResults.Add(transactionResult);
                        break;
                    default:
                        $"Transaction: {txId}, Status: {transactionStatus}".WriteWarningLine();
                        transactionIdQueue.Enqueue(txId);
                        break;    
                }

                await Task.Delay(10);
            }

            return transactionResults;
        }

        public string ConvertTransactionToRawInfo(Transaction transaction)
        {
            return transaction.ToByteArray().ToHex();
        }

        public void SetCheckTransactionResultTimeout(int seconds)
        {
            _checkTimeout = seconds;
        }

        private async Task<Transaction> AddBlockReference(Transaction transaction)
        {
            return await transaction.AddBlockReference(_apiService, _chainId);
        }
    }
}