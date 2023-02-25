using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Client.Service;
using AElf.CSharp.Core;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using log4net;
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

                TransactionResultStatus status;
                var txExist = CheckTransactionExisted(transaction, out var resultDto);
                if (txExist)
                {
                    status = resultDto.Status.ConvertTransactionResultStatus();
                    Logger.Warn("Duplicate transaction execution.");
                }
                else
                {
                    var transactionId = NodeManager.SendTransaction(transaction.ToByteArray().ToHex());
                    Logger.Info($"Transaction method: {transaction.MethodName}, TxId: {transactionId}");
                    resultDto = NodeManager.CheckTransactionResult(transactionId);
                    status = resultDto.Status.ConvertTransactionResultStatus();
                }

                var transactionResult = resultDto.Logs == null
                    ? new TransactionResult
                    {
                        TransactionId = Hash.LoadFromHex(resultDto.TransactionId),
                        BlockHash = resultDto.BlockHash == null
                            ? null
                            : HashHelper.ComputeFrom(resultDto.BlockHash),
                        BlockNumber = resultDto.BlockNumber,
                        Bloom = ByteString.CopyFromUtf8(resultDto.Bloom ?? ""),
                        Error = resultDto.Error ?? "",
                        Status = status,
                        ReturnValue = (resultDto.ReturnValue ?? "").ToByteString()
                    }
                    : new TransactionResult
                    {
                        TransactionId = Hash.LoadFromHex(resultDto.TransactionId),
                        BlockHash = resultDto.BlockHash == null
                            ? null
                            : HashHelper.ComputeFrom(resultDto.BlockHash),
                        BlockNumber = resultDto.BlockNumber,
                        Logs =
                        {
                            resultDto.Logs.Select(o =>
                            {
                                var logEvent = new LogEvent
                                {
                                    Address = o.Address.ConvertAddress(),
                                    Name = o.Name,
                                    NonIndexed = ByteString.FromBase64(o.NonIndexed),
                                };
                                foreach (var indexed in o.Indexed)
                                {
                                    logEvent.Indexed.Add(ByteString.FromBase64(indexed));
                                }
                                return logEvent;
                                
                            }).ToArray()
                        },
                        Bloom = ByteString.CopyFromUtf8(resultDto.Bloom ?? ""),
                        Error = resultDto.Error ?? "",
                        Status = status,
                        ReturnValue = (resultDto.ReturnValue ?? "").ToByteString()
                    };

                var returnByte = resultDto.ReturnValue == null
                    ? new byte[] { }
                    : ByteArrayHelper.HexStringToByteArray(resultDto.ReturnValue);
                return await Task.FromResult(new ExecutionResult<TOutput>
                {
                    Transaction = transaction,
                    TransactionResult = transactionResult,
                    Output = method.ResponseMarshaller.Deserializer(returnByte)
                });
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