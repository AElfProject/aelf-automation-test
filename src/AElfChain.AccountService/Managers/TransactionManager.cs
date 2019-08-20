using System;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using AElfChain.SDK;
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
        public ILog Logger { get; set; }

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
        
        public Transaction CreateTransaction(string @from, string to, string methodName, ByteString input)
        {
            try
            {
                var transaction = new Transaction
                {
                    From = AddressHelper.Base58StringToAddress(from),
                    To = AddressHelper.Base58StringToAddress(to),
                    MethodName = methodName,
                    Params = input ?? ByteString.Empty
                };

                return transaction;
            }
            catch (Exception e)
            {
                Logger.Error($"Invalid transaction data: {e.Message}");
                return null;
            }
        }

        public async Task<Transaction> AddBlockReference(Transaction transaction)
        {
            return await transaction.AddBlockReference(_apiService, _chainId);
        }

        public async Task<Transaction> SignTransaction(Transaction transaction, string password = "123")
        {
            if (transaction.RefBlockNumber == 0)
                transaction = await transaction.AddBlockReference(_apiService);
            
            var txData = transaction.GetHash().ToByteArray();
            transaction.Signature = await Sign(transaction.From.GetFormatted(), password, txData);

            return transaction;
        }

        public string ConvertTransactionToRawInfo(Transaction transaction)
        {
            return transaction.ToByteArray().ToHex();
        }
        
        private async Task<ByteString> Sign(string account, string password, byte[] txData)
        {
            var accountInfo = await _accountManager.GetAccountInfoAsync(account, password);

            // Sign the hash
            var signature = await _accountManager.SignAsync(accountInfo, txData);
            
            return ByteString.CopyFrom(signature);
        }
    }
}