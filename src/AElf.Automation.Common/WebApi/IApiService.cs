using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Automation.Common.WebApi.Dto;
using Google.Protobuf;

namespace AElf.Automation.Common.WebApi
{
    public interface IApiService
    {
        Task<string> ExecuteTransaction(string rawTransaction);

        Task<TResult> ExecuteTransaction<TResult>(string rawTransaction) where TResult : IMessage<TResult>, new();

        Task<byte[]> GetContractFileDescriptorSet(string address);

        Task<CreateRawTransactionOutput> CreateRawTransaction(CreateRawTransactionInput input);

        Task<SendRawTransactionOutput> SendRawTransaction(SendRawTransactionInput input);

        Task<SendTransactionOutput> SendTransaction(string rawTransaction);

        Task<string[]> SendTransactions(string rawTransactions);

        Task<TransactionResultDto> GetTransactionResult(string transactionId);

        Task<List<TransactionResultDto>> GetTransactionResults(string blockHash, int offset = 0, int limit = 10);

        Task<long> GetBlockHeight();

        Task<BlockDto> GetBlock(string blockHash, bool includeTransactions = false);

        Task<BlockDto> GetBlockByHeight(long blockHeight, bool includeTransactions = false);

        Task<GetTransactionPoolStatusOutput> GetTransactionPoolStatus();

        Task<ChainStatusDto> GetChainStatus();

        Task<BlockStateDto> GetBlockState(string blockHash);
        
        Task<List<TaskQueueInfoDto>> GetTaskQueueStatus();

        Task<RoundDto> GetCurrentRoundInformationAsync();
    }
}