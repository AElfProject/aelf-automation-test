using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.AccountService;
using AElfChain.SDK;
using AElfChain.SDK.Models;
using Google.Protobuf;
using Microsoft.Extensions.Logging;

namespace AElfChain.ContractService
{
    public class BaseContract : IContract
    {
        private readonly ITransactionManager _transactionManager;
        private readonly IAccountManager _accountManager;
        private readonly IApiService _apiService;

        public ILogger Logger { get; set; }
        public string Account { get; set; }

        public Address Contract { get; set; }

        public BaseContract(ITransactionManager transactionManager, IAccountManager accountManager, IApiService apiService, ILoggerFactory loggerFactory)
        {
            _transactionManager = transactionManager;
            _accountManager = accountManager;
            _apiService = apiService;
            Logger = loggerFactory.CreateLogger<BaseContract>();
        }
        
        public async Task SetContractExecutor(string account, string password = AccountOption.DefaultPassword)
        {
            Account = account;

            await _accountManager.UnlockAccountAsync(account, password);
        }

        public async Task<string> SendTransactionWithIdAsync(string method, IMessage input)
        {
            var transaction = await _transactionManager.CreateTransaction(Account, Contract.GetFormatted(), method, input.ToByteString());
            return await _transactionManager.SendTransactionWithIdAsync(transaction);
        }

        public Task<List<string>> SendBatchTransactionsWithIdAsync(List<string> rawInfos)
        {
            throw new System.NotImplementedException();
        }

        public Task<TransactionResultDto> SendTransactionWithResultAsync(string method, IMessage input)
        {
            throw new System.NotImplementedException();
        }

        public TResult CallTransactionAsync<TResult>(string method, IMessage input) where TResult : IMessage<TResult>
        {
            throw new System.NotImplementedException();
        }

        public TStub GetTestStub<TStub>(Address contract, string caller) where TStub : ContractStubBase
        {
            throw new System.NotImplementedException();
        }

        public TContract GetContractService<TContract>(Address contract, string caller) where TContract : IContract
        {
            throw new System.NotImplementedException();
        }
    }
}