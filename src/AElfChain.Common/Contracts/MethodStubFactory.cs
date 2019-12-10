using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using Google.Protobuf;
using log4net;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace AElfChain.Common.Contracts
{
    public class MethodStubFactory : IMethodStubFactory, ITransientDependency
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        public MethodStubFactory(INodeManager nodeManager)
        {
            NodeManager = nodeManager;
        }

        public Address Contract { private get; set; }
        public string SenderAddress { private get; set; }
        public Address Sender => SenderAddress.ConvertAddress();
        public INodeManager NodeManager { get; }
        public AElfClient ApiClient => NodeManager.ApiClient;

        public IMethodStub<TInput, TOutput> Create<TInput, TOutput>(Method<TInput, TOutput> method)
            where TInput : IMessage<TInput>, new() where TOutput : IMessage<TOutput>, new()
        {
            async Task<IExecutionResult<TOutput>> SendAsync(TInput input)
            {
                var transaction = new Transaction
                {
                    From = Sender,
                    To = Contract,
                    MethodName = method.Name,
                    Params = ByteString.CopyFrom(method.RequestMarshaller.Serializer(input))
                };
                transaction.AddBlockReference(NodeManager.GetApiUrl(), NodeManager.GetChainId());
                transaction = NodeManager.TransactionManager.SignTransaction(transaction);

                TransactionResultStatus status = TransactionResultStatus.Pending;
                var txExist = CheckTransactionExisted(transaction, out var resultDto);
                if (txExist)
                {
                    status = resultDto.Status.ConvertTransactionResultStatus();
                    Logger.Warn("Duplicate transaction execution.");
                }
                else
                {
                    var transactionId = NodeManager.SendTransaction(transaction.ToByteArray().ToHex());
                    await Task.Delay(2000); //delay for ovoid 'NotExisted' issue

                    var stopwatch = Stopwatch.StartNew();
                    var source = new CancellationTokenSource(5 * 60 * 1000); //check 5 minutes
                    while (!source.IsCancellationRequested)
                    {
                        resultDto = await ApiClient.GetTransactionResultAsync(transactionId);
                        status = resultDto.Status.ConvertTransactionResultStatus();
                        
                        if (status == TransactionResultStatus.Pending)
                        {
                            Console.Write(
                                $"\rTransaction {resultDto.TransactionId} status: {status}, time using: {CommonHelper.ConvertMileSeconds(stopwatch.ElapsedMilliseconds)}");
                            await Task.Delay(1000, source.Token);
                            continue;
                        }

                        if (status == TransactionResultStatus.NotExisted)
                        {
                            Logger.Error($"Transaction {transactionId} not existed");
                            break;
                        }

                        if (status == TransactionResultStatus.Mined)
                        {
                            Logger.Info(
                                $"TransactionId: {resultDto.TransactionId}, Method: {resultDto.Transaction.MethodName}, Status: {status}-[{resultDto.TransactionFee?.GetTransactionFeeInfo()}]",
                                true);
                            break;
                        }

                        if (status == TransactionResultStatus.Failed || status == TransactionResultStatus.Unexecutable)
                        {
                            Logger.Error(
                                $"TransactionId: {resultDto.TransactionId}, Method: {resultDto.Transaction.MethodName}, Status: {status}-[{resultDto.TransactionFee?.GetTransactionFeeInfo()}]\r\nDetail message: {JsonConvert.SerializeObject(resultDto, Formatting.Indented)}",
                                true);
                            break;
                        }
                    }

                    if (source.IsCancellationRequested && status == TransactionResultStatus.Pending)
                    {
                        Console.WriteLine();
                        throw new TimeoutException(
                            $"Transaction {resultDto.TransactionId} in '{status}' status long times.");
                    }
                    stopwatch.Stop();
                }

                var transactionFee = resultDto.TransactionFee.ConvertTransactionFeeDto();
                var transactionResult = resultDto.Logs == null
                    ? new TransactionResult
                    {
                        TransactionId = HashHelper.HexStringToHash(resultDto.TransactionId),
                        BlockHash = resultDto.BlockHash == null
                            ? null
                            : Hash.FromString(resultDto.BlockHash),
                        BlockNumber = resultDto.BlockNumber,
                        Bloom = ByteString.CopyFromUtf8(resultDto.Bloom ?? ""),
                        Error = resultDto.Error ?? "",
                        Status = status,
                        TransactionFee = transactionFee,
                        ReadableReturnValue = resultDto.ReadableReturnValue ?? ""
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
                                Address = o.Address.ConvertAddress(),
                                Name = o.Name,
                                NonIndexed = ByteString.FromBase64(o.NonIndexed)
                            }).ToArray()
                        },
                        Bloom = ByteString.CopyFromUtf8(resultDto.Bloom),
                        Error = resultDto.Error ?? "",
                        Status = status,
                        TransactionFee = transactionFee,
                        ReadableReturnValue = resultDto.ReadableReturnValue ?? ""
                    };

                var returnByte = resultDto.ReturnValue == null
                    ? new byte[] { }
                    : ByteArrayHelper.HexStringToByteArray(resultDto.ReturnValue);
                return new ExecutionResult<TOutput>
                {
                    Transaction = transaction,
                    TransactionResult = transactionResult,
                    Output = method.ResponseMarshaller.Deserializer(returnByte)
                };
            }

            async Task<TOutput> CallAsync(TInput input)
            {
                var transaction = new Transaction
                {
                    From = Sender,
                    To = Contract,
                    MethodName = method.Name,
                    Params = ByteString.CopyFrom(method.RequestMarshaller.Serializer(input))
                };
                transaction = NodeManager.TransactionManager.SignTransaction(transaction);

                var returnValue = await ApiClient.ExecuteTransactionAsync(new ExecuteTransactionDto
                    {
                        RawTransaction = transaction.ToByteArray().ToHex()
                        
                    });
                return method.ResponseMarshaller.Deserializer(ByteArrayHelper.HexStringToByteArray(returnValue));
            }

            return new MethodStub<TInput, TOutput>(method, SendAsync, CallAsync);
        }

        private bool CheckTransactionExisted(Transaction transaction, out TransactionResultDto transactionResult)
        {
            var txId = transaction.GetHash().ToHex();
            transactionResult = AsyncHelper.RunSync(() => ApiClient.GetTransactionResultAsync(txId));
            return transactionResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.NotExisted;
        }
    }
}