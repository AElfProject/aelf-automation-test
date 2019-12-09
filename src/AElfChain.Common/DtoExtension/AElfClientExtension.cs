using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.Client.Service;
using Volo.Abp.Threading;

namespace AElfChain.Common.DtoExtension
{
    public static class AElfClientExtension
    {
        /// <summary>
        /// 执行Action交易 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="rawTransaction"></param>
        /// <returns></returns>
        public static string SendTransaction(this AElfClient client, string rawTransaction)
        {
            return AsyncHelper.RunSync(() => client.SendTransactionAsync(new SendTransactionInput
            {
                RawTransaction = rawTransaction
            })).TransactionId;
        }

        /// <summary>
        /// 异步执行Action交易 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="rawTransaction"></param>
        /// <returns></returns>
        public static async Task<string> SendTransactionAsync(this AElfClient client, string rawTransaction)
        {
            return (await client.SendTransactionAsync(new SendTransactionInput
            {
                RawTransaction = rawTransaction
            })).TransactionId;
        }

        /// <summary>
        /// 查询View交易
        /// </summary>
        /// <param name="client"></param>
        /// <param name="rawTransaction"></param>
        /// <returns></returns>
        public static string ExecuteTransaction(this AElfClient client, string rawTransaction)
        {
            return AsyncHelper.RunSync(() => client.ExecuteTransactionAsync(new ExecuteTransactionDto
            {
                RawTransaction = rawTransaction
            }));
        }
        
        /// <summary>
        /// 实例化AElf Client
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