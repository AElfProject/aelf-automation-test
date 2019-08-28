using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.AccountService;
using AElfChain.SDK;
using AElfChain.SDK.Models;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;

namespace AElfChain.ContractService
{
    public class MethodStubFactory : IMethodStubFactory, ITransientDependency
    {
        public ILogger Logger { get; set; }
        
        private readonly ITransactionManager _transactionManager;
        private readonly IApiService _apiService;

        public Address Contract { get; set; }
        public AccountInfo Account { get; set; }

        public MethodStubFactory(ITransactionManager transactionManager, IApiService apiService, ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory.CreateLogger<MethodStubFactory>();
            _transactionManager = transactionManager;
            _apiService = apiService;
        }

        public MethodStubFactory GetMethodStubFactory(Address contract, AccountInfo account)
        {
            Contract = contract;
            Account = account;

            return this;
        }
        
        public IMethodStub<TInput, TOutput> Create<TInput, TOutput>(Method<TInput, TOutput> method)
            where TInput : IMessage<TInput>, new() where TOutput : IMessage<TOutput>, new()
        {
            async Task<IExecutionResult<TOutput>> SendAsync(TInput input)
            {
                var transaction = await _transactionManager.CreateTransaction(Account.Formatted, Contract.GetFormatted(), method.Name, input.ToByteString());
                var transactionId = await _transactionManager.SendTransactionWithIdAsync(transaction);

                var checkTimes = 0;
                TransactionResultDto resultDto;
                TransactionResultStatus status;
                while (true)
                {
                    checkTimes++;
                    resultDto = await _apiService.GetTransactionResultAsync(transactionId);
                    status = resultDto.Status.ConvertTransactionResultStatus();
                    if (status != TransactionResultStatus.Pending && status != TransactionResultStatus.NotExisted)
                    {
                        if (status == TransactionResultStatus.Mined)
                            Logger.LogInformation($"TransactionId: {resultDto.TransactionId}, Method: {resultDto.Transaction.MethodName}, Status: {status}");
                        else
                            Logger.LogError(
                                $"TransactionId: {resultDto.TransactionId}, Status: {status}\r\nDetail message: {JsonConvert.SerializeObject(resultDto)}");

                        break;
                    }

                    if (checkTimes == 360) //max wait time 3 minutes
                        throw new Exception($"Transaction {resultDto.TransactionId} in pending status more than one minutes.");
                    Thread.Sleep(500);
                }

                var readableReturnValue = string.Empty;
                ByteString returnValue = null;
                if (resultDto.ReadableReturnValue != null)
                {
                    readableReturnValue = resultDto.ReadableReturnValue.Replace("\"", "");
                    returnValue = ByteString.FromBase64(readableReturnValue);
                }
                var transactionResult = resultDto.Logs == null
                    ? new TransactionResult
                    {
                        TransactionId = HashHelper.HexStringToHash(resultDto.TransactionId),
                        BlockHash = resultDto.BlockHash == null
                            ? null
                            : Hash.FromString(resultDto.BlockHash),
                        BlockNumber = resultDto.BlockNumber,
                        Bloom = ByteString.CopyFromUtf8(resultDto.Bloom),
                        Error = resultDto.Error ?? "",
                        Status = status,
                        ReadableReturnValue = readableReturnValue,
                        ReturnValue = returnValue
                    }
                    : new TransactionResult
                    {
                        TransactionId = HashHelper.HexStringToHash(resultDto.TransactionId),
                        BlockHash = resultDto.BlockHash == null
                            ? null
                            : Hash.FromString(resultDto.BlockHash),
                        BlockNumber = resultDto.BlockNumber,
                        Logs =
                        {
                            resultDto.Logs.Select(o => new LogEvent
                            {
                                Address = AddressHelper.Base58StringToAddress(o.Address),
                                Name = o.Name,
                                NonIndexed = ByteString.FromBase64(o.NonIndexed)
                            }).ToArray()
                        },
                        Bloom = ByteString.CopyFromUtf8(resultDto.Bloom),
                        Error = resultDto.Error ?? "",
                        Status = status,
                        ReadableReturnValue = readableReturnValue,
                        ReturnValue = returnValue
                    };

                return new ExecutionResult<TOutput>()
                {
                    Transaction = transaction,
                    TransactionResult = transactionResult,
                    Output = method.ResponseMarshaller.Deserializer(transactionResult.ToByteArray())
                };
            }

            async Task<TOutput> CallAsync(TInput input)
            {
                var transaction = await _transactionManager.CreateTransaction(Account.Formatted, Contract.GetFormatted(), method.Name, input.ToByteString());
                var returnValue = await _apiService.ExecuteTransactionAsync(transaction.ToByteArray().ToHex());
                
                return method.ResponseMarshaller.Deserializer(ByteArrayHelper.HexStringToByteArray(returnValue));
            }

            return new MethodStub<TInput, TOutput>(method, SendAsync, CallAsync);
        }
    }
}