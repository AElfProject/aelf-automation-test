using System.Threading.Tasks;
using AElf.Client.Dto;

namespace AElf.Client.Service
{
    public interface IBlockAppService
    {
        Task<long> GetBlockHeightAsync();

        Task<BlockDto> GetBlockByHashAsync(string blockHash, bool includeTransactions = false);

        Task<BlockDto> GetBlockByHeightAsync(long blockHeight, bool includeTransactions = false);
    }

    public partial class AElfClient : IBlockAppService
    {
        /// <summary>
        /// Get height of the current chain.
        /// </summary>
        /// <returns>Height</returns>
        public async Task<long> GetBlockHeightAsync()
        {
            var url = GetRequestUrl(BaseUrl, "api/blockChain/blockHeight");
            return await _httpService.GetResponseAsync<long>(url);
        }

        /// <summary>
        /// Get information of a block by given block hash. Optional whether to include transaction information.
        /// </summary>
        /// <param name="blockHash"></param>
        /// <param name="includeTransactions"></param>
        /// <returns>Block information</returns>
        public async Task<BlockDto> GetBlockByHashAsync(string blockHash, bool includeTransactions = false)
        {
            AssertValidHash(blockHash);
            var url = GetRequestUrl(BaseUrl,
                $"api/blockChain/block?blockHash={blockHash}&includeTransactions={includeTransactions}");
            return await _httpService.GetResponseAsync<BlockDto>(url);
        }

        /// <summary>
        /// Get information of a block by specified height. Optional whether to include transaction information.
        /// </summary>
        /// <param name="blockHeight"></param>
        /// <param name="includeTransactions"></param>
        /// <returns>Block information</returns>
        public async Task<BlockDto> GetBlockByHeightAsync(long blockHeight, bool includeTransactions = false)
        {
            var url = GetRequestUrl(BaseUrl,
                $"api/blockChain/blockByHeight?blockHeight={blockHeight}&includeTransactions={includeTransactions}");
            return await _httpService.GetResponseAsync<BlockDto>(url);
        }
    }
}