using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Client.Dto;

namespace AElf.Client.Service
{
    public interface ITransactionAppService
    {
        Task<TransactionPoolStatusOutput> GetTransactionPoolStatusAsync();

        Task<string> ExecuteTransactionAsync(ExecuteTransactionDto input);

        Task<string> ExecuteRawTransactionAsync(ExecuteRawTransactionDto input);

        Task<CreateRawTransactionOutput> CreateRawTransactionAsync(CreateRawTransactionInput input);

        Task<SendRawTransactionOutput> SendRawTransactionAsync(SendRawTransactionInput input);

        Task<SendTransactionOutput> SendTransactionAsync(SendTransactionInput input);

        Task<string[]> SendTransactionsAsync(SendTransactionsInput input);

        Task<TransactionResultDto> GetTransactionResultAsync(string transactionId);

        Task<List<TransactionResultDto>> GetTransactionResultsAsync(string blockHash, int offset = 0,
            int limit = 10);

        Task<MerklePathDto> GetMerklePathByTransactionIdAsync(string transactionId);
    }

    public partial class AElfClient : ITransactionAppService
    {
        /// <summary>
        /// Get information about the current transaction pool.
        /// </summary>
        /// <returns>TransactionPoolStatusOutput</returns>
        public async Task<TransactionPoolStatusOutput> GetTransactionPoolStatusAsync()
        {
            var url = GetRequestUrl(BaseUrl, "api/blockChain/transactionPoolStatus");
            return await _httpService.GetResponseAsync<TransactionPoolStatusOutput>(url);
        }

        /// <summary>
        /// Call a read-only method of a contract.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<string> ExecuteTransactionAsync(ExecuteTransactionDto input)
        {
            var url = GetRequestUrl(BaseUrl, "api/blockChain/executeTransaction");
            var parameters = new Dictionary<string, string>
            {
                {"RawTransaction", input.RawTransaction}
            };

            return await _httpService.PostResponseAsync<string>(url, parameters);
        }

        /// <summary>
        /// Call a method of a contract by given serialized strings.
        /// </summary>
        /// <param name="input"></param>
        /// <returns>Serialized result</returns>
        public async Task<string> ExecuteRawTransactionAsync(ExecuteRawTransactionDto input)
        {
            var url = GetRequestUrl(BaseUrl, "api/blockChain/executeRawTransaction");
            var parameters = new Dictionary<string, string>
            {
                {"RawTransaction", input.RawTransaction},
                {"Signature", input.Signature}
            };

            return await _httpService.PostResponseAsync<string>(url, parameters);
        }

        /// <summary>
        /// Creates an unsigned serialized transaction.
        /// </summary>
        /// <param name="input"></param>
        /// <returns>CreateRawTransactionOutput</returns>
        public async Task<CreateRawTransactionOutput> CreateRawTransactionAsync(CreateRawTransactionInput input)
        {
            var url = GetRequestUrl(BaseUrl, "api/blockChain/rawTransaction");
            var parameters = new Dictionary<string, string>
            {
                {"From", input.From},
                {"To", input.To},
                {"RefBlockNumber", input.RefBlockNumber.ToString()},
                {"RefBlockHash", input.RefBlockHash},
                {"MethodName", input.MethodName},
                {"Params", input.Params}
            };

            return await _httpService.PostResponseAsync<CreateRawTransactionOutput>(url, parameters);
        }

        /// <summary>
        /// Broadcast a serialized transaction.
        /// </summary>
        /// <param name="input"></param>
        /// <returns>SendRawTransactionOutput</returns>
        public async Task<SendRawTransactionOutput> SendRawTransactionAsync(SendRawTransactionInput input)
        {
            var url = GetRequestUrl(BaseUrl, "api/blockChain/sendRawTransaction");
            var parameters = new Dictionary<string, string>
            {
                {"Transaction", input.Transaction},
                {"Signature", input.Signature},
                {"ReturnTransaction", input.ReturnTransaction ? "true" : "false"}
            };
            return await _httpService.PostResponseAsync<SendRawTransactionOutput>(url, parameters);
        }

        /// <summary>
        /// Broadcast a transaction.
        /// </summary>
        /// <param name="input"></param>
        /// <returns>TransactionId</returns>
        public async Task<SendTransactionOutput> SendTransactionAsync(SendTransactionInput input)
        {
            var url = GetRequestUrl(BaseUrl, "api/blockChain/sendTransaction");
            var parameters = new Dictionary<string, string>
            {
                {"RawTransaction", input.RawTransaction}
            };
            return await _httpService.PostResponseAsync<SendTransactionOutput>(url, parameters);
        }

        /// <summary>
        /// Broadcast volume transactions.
        /// </summary>
        /// <param name="input"></param>
        /// <returns>TransactionIds</returns>
        public async Task<string[]> SendTransactionsAsync(SendTransactionsInput input)
        {
            var url = GetRequestUrl(BaseUrl, "api/blockChain/sendTransactions");
            var parameters = new Dictionary<string, string>
            {
                {"RawTransactions", input.RawTransactions}
            };
            return await _httpService.PostResponseAsync<string[]>(url, parameters);
        }

        /// <summary>
        /// Gets the result of transaction execution by the given transactionId.
        /// </summary>
        /// <param name="transactionId"></param>
        /// <returns>TransactionResultDto</returns>
        public async Task<TransactionResultDto> GetTransactionResultAsync(string transactionId)
        {
            AssertValidTransactionId(transactionId);
            var url = GetRequestUrl(BaseUrl, $"api/blockChain/transactionResult?transactionId={transactionId}");
            return await _httpService.GetResponseAsync<TransactionResultDto>(url);
        }

        /// <summary>
        /// Get results of multiple transactions by specified blockHash and the offset.
        /// </summary>
        /// <param name="blockHash"></param>
        /// <param name="offset"></param>
        /// <param name="limit"></param>
        /// <returns>TransactionResultDtos</returns>
        public async Task<List<TransactionResultDto>> GetTransactionResultsAsync(string blockHash, int offset = 0,
            int limit = 10)
        {
            AssertValidHash(blockHash);
            var url = GetRequestUrl(BaseUrl,
                $"api/blockChain/transactionResults?blockHash={blockHash}&offset={offset}&limit={limit}");
            return await _httpService.GetResponseAsync<List<TransactionResultDto>>(url);
        }

        /// <summary>
        /// Get merkle path of a transaction.
        /// </summary>
        /// <param name="transactionId"></param>
        /// <returns>MerklePathDto</returns>
        public async Task<MerklePathDto> GetMerklePathByTransactionIdAsync(string transactionId)
        {
            AssertValidTransactionId(transactionId);
            var url = GetRequestUrl(BaseUrl, $"api/blockChain/merklePathByTransactionId?transactionId={transactionId}");
            return await _httpService.GetResponseAsync<MerklePathDto>(url);
        }
    }
}