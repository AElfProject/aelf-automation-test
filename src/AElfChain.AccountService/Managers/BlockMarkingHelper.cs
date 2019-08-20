using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using AElfChain.SDK;
using AElfChain.SDK.Models;
using Google.Protobuf;

namespace AElfChain.AccountService
{
    public static class BlockMarkingHelper
    {
        private static DateTime _refBlockTime = DateTime.Now;
        private static long _cachedHeight;
        private static string _cachedHash;
        private static string _chainId = string.Empty;
        
        public static async Task<Transaction> AddBlockReference(this Transaction transaction, IApiService apiService,
            string chainId = "AELF")
        {
            if (_cachedHeight == default(long) || (DateTime.Now - _refBlockTime).TotalSeconds > 60 ||
                !_chainId.Equals(chainId))
            {
                _chainId = chainId;
                _cachedHeight = await GetBlockHeight(apiService);
                _cachedHash = await GetBlockHash(apiService, _cachedHeight);
                _refBlockTime = DateTime.Now;
            }

            transaction.RefBlockNumber = _cachedHeight;
            transaction.RefBlockPrefix =
                ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(_cachedHash).Where((b, i) => i < 4).ToArray());
            return transaction;
        }

        private static async Task<long> GetBlockHeight(IApiService apiService, int requestTimes = 3)
        {
            while (true)
            {
                requestTimes--;
                var height = await apiService.GetBlockHeightAsync();
                if (height != 0) return height;

                if (requestTimes < 0) throw new Exception("Get Block height failed exception.");
                Thread.Sleep(200);
            }
        }

        private static async Task<string> GetBlockHash(IApiService apiService, long height, int requestTimes = 3)
        {
            while (true)
            {
                requestTimes--;
                var blockInfo = await apiService.GetBlockByHeightAsync(height);
                if (blockInfo != null && blockInfo != new BlockDto()) return blockInfo.BlockHash;

                if (requestTimes < 0) throw new Exception("Get Block hash failed exception.");
                Thread.Sleep(200);
            }
        }
    }
}