using Volo.Abp.Threading;

namespace AElfChain.Common.Utils
{
//    public class AElfChainClient
//    {
//        public static AElfClient GetClient(string baseUrl)
//        {
//            var endpoint = FormatServiceUrl(baseUrl);
//            return new AElfClient(endpoint);
//        }
//
//        private static string FormatServiceUrl(string baseUrl)
//        {
//            if (baseUrl.Contains("http://") || baseUrl.Contains("https://"))
//                return baseUrl;
//
//            return $"http://{baseUrl}";
//        }
//    }
//
//    public static class AElfClientExtension
//    {
//        public static string SendTransactionAsync(this AElfClient client, string rawTransaction)
//        {
//            return AsyncHelper.RunSync(() => client.ExecuteTransactionAsync(new ExecuteTransactionDto
//            {
//                RawTransaction = rawTransaction
//            }));
//        }
//    }
}