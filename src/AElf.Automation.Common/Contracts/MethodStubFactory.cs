using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.WebApi;
using AElf.Automation.Common.WebApi.Dto;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.Automation.Common.Contracts
{
    public class MethodStubFactory : IMethodStubFactory, ITransientDependency
    {
        public ECKeyPair KeyPair { get; set; } = CryptoHelpers.GenerateKeyPair();

        public Address ContractAddress { get; set; }

        public Address Sender => Address.FromPublicKey(KeyPair.PublicKey);
        
        public string BaseUrl { get; set; }
        
        public WebApiService ApiService { get; set; }

        public MethodStubFactory(string baseUrl)
        {
            BaseUrl = baseUrl;
            ApiService = new WebApiService(BaseUrl);
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
                transaction.AddBlockReference(BaseUrl);
                
                var signature = CryptoHelpers.SignWithPrivateKey(
                    KeyPair.PrivateKey, transaction.GetHash().Value.ToByteArray());
                transaction.Signature = ByteString.CopyFrom(signature);
                await ApiService.BroadcastTransaction(transaction.ToByteArray().ToHex());
                var transactionResult = await ApiService.GetTransactionResult(transaction.GetHash().ToHex());
                return new ExecutionResult<TOutput>()
                {
                    Transaction = transaction, 
                    TransactionResult = new TransactionResult
                    {
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
                        Error = transactionResult.Error,
                        Status = (TransactionResultStatus)Enum.Parse(typeof(TransactionResultStatus), transactionResult.Status),
                    },
                    Output = method.ResponseMarshaller.Deserializer(ByteArrayHelpers.FromHexString(transactionResult.ReadableReturnValue))
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
                var returnValue = await ApiService.Call(transaction.ToByteArray().ToHex());
                return method.ResponseMarshaller.Deserializer(ByteArrayHelpers.FromHexString(returnValue));
            }

            return new MethodStub<TInput, TOutput>(method, SendAsync, CallAsync);
        }
    }
}