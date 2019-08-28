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
        private readonly IContractFactory _contractFactory;
        private readonly ITransactionManager _transactionManager;
        private readonly IAccountManager _accountManager;

        public ILogger Logger { get; set; }
        public AccountInfo AccountInfo { get; set; }

        public Address Contract { get; set; }

        public BaseContract(IContractFactory contractFactory, ITransactionManager transactionManager, IAccountManager accountManager, ILoggerFactory loggerFactory)
        {
            _contractFactory = contractFactory;
            _transactionManager = transactionManager;
            _accountManager = accountManager;
            Logger = loggerFactory.CreateLogger<BaseContract>();
        }

        public TContract GetContractService<TContract>(Address contract, AccountInfo accountInfo) where TContract : IContract
        {
            throw new System.NotImplementedException();
        }

        public async Task SetContractExecutor(string account, string password = AccountOption.DefaultPassword)
        {
            AccountInfo = await _accountManager.GetAccountInfoAsync(account, password);
        }

        public async Task<string> SendTransactionWithIdAsync(string method, IMessage input)
        {
            var transaction = await _transactionManager.CreateTransaction(AccountInfo.Formatted, Contract.GetFormatted(), method, input.ToByteString());
            return await _transactionManager.SendTransactionWithIdAsync(transaction);
        }

        public async Task<List<string>> SendBatchTransactionsWithIdAsync(List<Transaction> transactions)
        {
            return await _transactionManager.SendBatchTransactionWithIdAsync(transactions);
        }

        public Task<TransactionResultDto> SendTransactionWithResultAsync(string method, IMessage input)
        {
            throw new System.NotImplementedException();
        }

        public TResult CallTransactionAsync<TResult>(string method, IMessage input) where TResult : IMessage<TResult>
        {
            throw new System.NotImplementedException();
        }

        TStub IContract.GetTestStub<TStub>(Address contract, AccountInfo accountInfo)
        {
            throw new System.NotImplementedException();
        }

        public string GetContractFileName()
        {
            throw new System.NotImplementedException();
        }

        public TContract DeployContract<TContract>(AccountInfo accountInfo) where TContract : IContract
        {
            throw new System.NotImplementedException();
        }

        public TStub GetTestStub<TStub>(Address contract, AccountInfo accountInfo) where TStub : ContractStubBase, new()
        {
            var contractStub = _contractFactory.Create<TStub>(contract, accountInfo);
            return contractStub;
        }
    }
}