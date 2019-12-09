using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Client.Dto;

namespace AElf.Client.Service
{
    public interface IChainAppService
    {
        Task<ChainStatusDto> GetChainStatusAsync();

        Task<byte[]> GetContractFileDescriptorSetAsync(string address);

        Task<RoundDto> GetCurrentRoundInformationAsync();

        Task<List<TaskQueueInfoDto>> GetTaskQueueStatusAsync();

        Task<int> GetChainIdAsync();
    }

    public partial class AElfClient : IChainAppService
    {
        /// <summary>
        /// Get the current status of the block chain.
        /// </summary>
        /// <returns>Description of current status</returns>
        public async Task<ChainStatusDto> GetChainStatusAsync()
        {
            var url = GetRequestUrl(BaseUrl, "api/blockChain/chainStatus");
            return await _httpService.GetResponseAsync<ChainStatusDto>(url);
        }

        /// <summary>
        /// Get the definitions of proto-buff related to a contract.
        /// </summary>
        /// <param name="address"></param>
        /// <returns>Definitions of proto-buff</returns>
        public async Task<byte[]> GetContractFileDescriptorSetAsync(string address)
        {
            AssertValidAddress(address);
            var url = GetRequestUrl(BaseUrl, $"api/blockChain/contractFileDescriptorSet?address={address}");
            return await _httpService.GetResponseAsync<byte[]>(url);
        }

        /// <summary>
        /// Get the latest round of consensus information from data on the last blockHeader of best-chain.
        /// </summary>
        /// <returns>The latest round of consensus information</returns>
        public async Task<RoundDto> GetCurrentRoundInformationAsync()
        {
            var url = GetRequestUrl(BaseUrl, "api/blockChain/currentRoundInformation");
            return await _httpService.GetResponseAsync<RoundDto>(url);
        }

        /// <summary>
        /// Gets the status information of the task queue.
        /// </summary>
        /// <returns>Information of the task queue</returns>
        public async Task<List<TaskQueueInfoDto>> GetTaskQueueStatusAsync()
        {
            var url = GetRequestUrl(BaseUrl, "api/blockChain/taskQueueStatus");
            return await _httpService.GetResponseAsync<List<TaskQueueInfoDto>>(url);
        }

        /// <summary>
        /// Get id of the chain.
        /// </summary>
        /// <returns>ChainId</returns>
        public async Task<int> GetChainIdAsync()
        {
            var url = GetRequestUrl(BaseUrl, "api/blockChain/chainStatus");
            var statusDto = await _httpService.GetResponseAsync<ChainStatusDto>(url);
            var base58ChainId = statusDto.ChainId;
            var chainId = ChainHelper.ConvertBase58ToChainId(base58ChainId);

            return chainId;
        }
    }
}