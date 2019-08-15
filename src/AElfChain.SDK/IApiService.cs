using System.Collections.Generic;
using System.Threading.Tasks;
using AElfChain.SDK.Models;
using Google.Protobuf;

namespace AElfChain.SDK
{
    public interface IApiService
    {
        string GetServiceUrl();
        Task<string> ExecuteTransactionAsync(string rawTransaction);

        Task<TResult> ExecuteTransactionAsync<TResult>(string rawTransaction) where TResult : IMessage<TResult>, new();

        Task<byte[]> GetContractFileDescriptorSetAsync(string address);

        Task<CreateRawTransactionOutput> CreateRawTransactionAsync(CreateRawTransactionInput input);

        Task<SendRawTransactionOutput> SendRawTransactionAsync(SendRawTransactionInput input);

        Task<SendTransactionOutput> SendTransactionAsync(string rawTransaction);

        Task<string[]> SendTransactionsAsync(string rawTransactions);

        Task<TransactionResultDto> GetTransactionResultAsync(string transactionId);

        Task<List<TransactionResultDto>> GetTransactionResultsAsync(string blockHash, int offset = 0, int limit = 10);

        Task<long> GetBlockHeightAsync();

        Task<BlockDto> GetBlockAsync(string blockHash, bool includeTransactions = false);

        Task<BlockDto> GetBlockByHeightAsync(long blockHeight, bool includeTransactions = false);

        Task<GetTransactionPoolStatusOutput> GetTransactionPoolStatusAsync();

        Task<ChainStatusDto> GetChainStatusAsync();

        Task<BlockStateDto> GetBlockStateAsync(string blockHash);

        Task<List<TaskQueueInfoDto>> GetTaskQueueStatusAsync();

        Task<RoundDto> GetCurrentRoundInformationAsync();

        Task<bool> AddPeerAsync(string address);

        Task<bool> RemovePeerAsync(string address);

        Task<List<PeerDto>> GetPeersAsync();
    }
}