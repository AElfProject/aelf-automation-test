using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Automation.Common.Helpers;
using AElf.Types;
using AElfChain.SDK;
using AElfChain.SDK.Models;
using Google.Protobuf;
using log4net;
using Volo.Abp.Threading;

namespace AElfChain.AccountService
{
    public class TransactionManager : ITransactionManager
    {
        private readonly IAccountManager _accountManager;
        private readonly IApiService _apiService;

        private string _chainId;
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
            return transactionOutput.TransactionId;
        }

        public async Task<List<string>> SendBatchTransactionWithIdAsync(List<Transaction> transactions)
        {
            var rawTransactions = string.Join(",", transactions.Select(o => o.ToByteArray().ToHex()));
            var txIds = await _apiService.SendTransactionsAsync(rawTransactions);

            return txIds.ToList();
        }

        public async Task<TransactionResultDto> QueryTransactionAsync(string transactionId)
        {
            var transactionResult =  await _apiService.GetTransactionResultAsync(transactionId);
            return transactionResult;
        }

        public string ConvertTransactionToRawInfo(Transaction transaction)
        {
            return transaction.ToByteArray().ToHex();
        }

        private async Task<Transaction> AddBlockReference(Transaction transaction)
        {
            return await transaction.AddBlockReference(_apiService, _chainId);
        }
    }
}