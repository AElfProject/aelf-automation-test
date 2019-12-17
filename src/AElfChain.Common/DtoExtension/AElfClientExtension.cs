using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.Client.Service;
using Volo.Abp.Threading;

namespace AElfChain.Common.DtoExtension
{
    public static class AElfClientExtension
    {
        /// <summary>
        /// get AElf client instance
        /// </summary>
        /// <param name="baseUrl"></param>
        /// <returns></returns>
        public static AElfClient GetClient(string baseUrl)
        {
            var endpoint = FormatServiceUrl(baseUrl);
            return new AElfClient(endpoint);
        }
        
        private static string FormatServiceUrl(string baseUrl)
        {
            if (baseUrl.Contains("http://") || baseUrl.Contains("https://"))
                return baseUrl;

            return $"http://{baseUrl}";
        }
    }
}