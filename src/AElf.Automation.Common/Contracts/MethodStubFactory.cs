using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi;
using AElf.Automation.Common.WebApi.Dto;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Volo.Abp.DependencyInjection;

namespace AElf.Automation.Common.Contracts
{
    public class MethodStubFactory : IMethodStubFactory, ITransientDependency
    {
        public Address ContractAddress { get; set; }
        public string SenderAddress { get; set; }
        public Address Sender { get; set; }
        public WebApiHelper ApiHelper { get; }
        public WebApiService ApiService { get; }
        
        private readonly string _baseUrl;

        public MethodStubFactory(string baseUrl, string keyPath = "")
        {
            _baseUrl = baseUrl;
            
            ApiHelper = new WebApiHelper(baseUrl, keyPath);
            ApiService = ApiHelper.ApiService;
            
            ApiHelper.GetChainInformation(new CommandInfo(ApiMethods.GetChainInformation));
        }
        
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public IMethodStub<TInput, TOutput> Create<TInput, TOutput>(Method<TInput, TOutput> method)
            where TInput : IMessage<TInput>, new() where TOutput : IMessage<TOutput>, new()
        {
            async Task<IExecutionResult<TOutput>> SendAsync(TInput input)
            {
                var transaction = new Transaction()
                {
                    From = Sender,
                    To = ContractAddress,
                    MethodName = method.Name,
                    Params = ByteString.CopyFrom(method.RequestMarshaller.Serializer(input)),
                };
                transaction.AddBlockReference(_baseUrl);
                transaction = ApiHelper.TransactionManager.SignTransaction(transaction);
                
                var transactionOutput =  await ApiService.BroadcastTransaction(transaction.ToByteArray().ToHex());
                
                var checkTimes = 0;
                TransactionResultDto transactionResult;
                while (true)
                {
                    checkTimes++;
                    transactionResult = await ApiService.GetTransactionResult(transactionOutput.TransactionId);
                    var status =
                        (TransactionResultStatus) Enum.Parse(typeof(TransactionResultStatus), transactionResult.Status);
                    if (status != TransactionResultStatus.Pending)
                        break;
                    
                    if(checkTimes == 120)
                        Assert.IsTrue(false, $"Transaction {transactionResult.TransactionId} in pending status more than one minutes.");
                    Thread.Sleep(500);
                }

                return new ExecutionResult<TOutput>()
                {
                    Transaction = transaction, 
                    TransactionResult = new TransactionResult
                    {
                        TransactionId = Hash.LoadHex(transactionResult.TransactionId),
                        BlockHash = Hash.FromString(transactionResult.BlockHash),
                        BlockNumber = transactionResult.BlockNumber,
                        Logs = { 
                            transactionResult.Logs.Select(o => new LogEvent
                            {
                                Address = Address.Parse(o.Address),
                                Name = o.Name,
                                NonIndexed = ByteString.CopyFromUtf8(o.NonIndexed)
                            }).ToList()
                        },
                        Bloom = ByteString.CopyFromUtf8(transactionResult.Bloom),
                        Error = transactionResult.Error ?? "",
                        Status = (TransactionResultStatus)Enum.Parse(typeof(TransactionResultStatus), transactionResult.Status),
                        ReadableReturnValue = transactionResult.ReadableReturnValue
                    }
                };
            }
            
            async Task<TOutput> CallAsync(TInput input)
            {
                var transaction = new Transaction()
                {
                    From = Sender,
                    To = ContractAddress,
                    MethodName = method.Name,
                    Params = ByteString.CopyFrom(method.RequestMarshaller.Serializer(input))
                };
                transaction = ApiHelper.TransactionManager.SignTransaction(transaction);
                
                var returnValue = await ApiService.Call(transaction.ToByteArray().ToHex());
                return method.ResponseMarshaller.Deserializer(ByteArrayHelpers.FromHexString(returnValue));
            }

            return new MethodStub<TInput, TOutput>(method, SendAsync, CallAsync);
        }
    }
}