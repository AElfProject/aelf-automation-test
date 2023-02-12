using AElf.Client.Service;
using AElf.Client.Service;

namespace AElfChain.Common.DtoExtension
{
    public static class AElfClientExtension
    {
        /// <summary>
        ///     get AElf client instance
        /// </summary>
        /// <param name="baseUrl"></param>
        /// <returns></returns>
        public static AElfClient GetClient(string baseUrl)
        {
            var endpoint = FormatServiceUrl(baseUrl);
            return new AElfClient(endpoint, 60, "aelf", "12345678");
        }

        private static string FormatServiceUrl(string baseUrl)
        {
            if (baseUrl.Contains("http://") || baseUrl.Contains("https://"))
                return baseUrl;

            return $"http://{baseUrl}";
        }
    }
}