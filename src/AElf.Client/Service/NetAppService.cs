using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Client.Dto;

namespace AElf.Client.Service
{
    public interface INetAppService
    {
        Task<bool> AddPeerAsync(string address);

        Task<bool> RemovePeerAsync(string address);

        Task<List<PeerDto>> GetPeersAsync(bool withMetrics);

        Task<NetworkInfoOutput> GetNetworkInfoAsync();
    }

    public partial class AElfClient : INetAppService
    {
        /// <summary>
        /// Attempt to add a node to the connected network nodes.Input parameter contains the ipAddress of the node.
        /// </summary>
        /// <param name="address"></param>
        /// <returns>Add successfully or not</returns>
        public async Task<bool> AddPeerAsync(string address)
        {
            var url = GetRequestUrl(BaseUrl, "api/net/peer");
            var parameters = new Dictionary<string, string>
            {
                {"address", address}
            };

            return await _httpService.PostResponseAsync<bool>(url, parameters);
        }

        /// <summary>
        /// Attempt to remove a node from the connected network nodes by given the ipAddress.
        /// </summary>
        /// <param name="address"></param>
        /// <returns>Delete successfully or not</returns>
        public async Task<bool> RemovePeerAsync(string address)
        {
            var url = GetRequestUrl(BaseUrl, $"api/net/peer?address={address}");
            return await _httpService.DeleteResponseAsObjectAsync<bool>(url);
        }

        /// <summary>
        /// Gets information about the peer nodes of the current node.Optional whether to include metrics.
        /// </summary>
        /// <param name="withMetrics"></param>
        /// <returns>Information about the peer nodes</returns>
        public async Task<List<PeerDto>> GetPeersAsync(bool withMetrics)
        {
            var url = GetRequestUrl(BaseUrl, $"api/net/peers?withMetrics={withMetrics}");
            return await _httpService.GetResponseAsync<List<PeerDto>>(url);
        }

        /// <summary>
        /// Get the node's network information.
        /// </summary>
        /// <returns>Network information</returns>
        public async Task<NetworkInfoOutput> GetNetworkInfoAsync()
        {
            var url = GetRequestUrl(BaseUrl, "api/net/networkInfo");
            return await _httpService.GetResponseAsync<NetworkInfoOutput>(url);
        }
    }
}