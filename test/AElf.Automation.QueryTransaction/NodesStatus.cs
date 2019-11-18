using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElfChain.Common.Helpers;
using AElfChain.SDK;
using AElfChain.SDK.Models;
using log4net;
using Nito.AsyncEx;

namespace AElf.Automation.QueryTransaction
{
    public class NodesStatus
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();
        private readonly List<IApiService> _apiServices;
        private long _height = 1;

        public NodesStatus(IEnumerable<string> urls)
        {
            _apiServices = new List<IApiService>();
            foreach (var url in urls)
            {
                var nodeManager = AElfChainClient.GetClient(url);
                _apiServices.Add(nodeManager);
            }
        }

        public async Task CheckAllNodes()
        {
            while (true)
            {
                var currentHeight = await _apiServices.First().GetBlockHeightAsync();
                if (currentHeight == _height) Thread.Sleep(1000);

                for (var i = _height; i < currentHeight; i++) await CheckNodeHeight(i);

                _height = currentHeight;
            }
        }

        private async Task CheckNodeHeight(long height)
        {
            var collection = new List<(IApiService, BlockDto)>();
            await _apiServices.AsParallel().Select(async api =>
            {
                var block = await api.GetBlockByHeightAsync(height);
                collection.Add((api, block));
                return collection;
            }).WhenAll();

            var (webApiService, blockDto) = collection.First();
            Logger.Info($"Check height: {height}");
            Logger.Info(
                $"Node: {webApiService.GetServiceUrl()}, Block hash: {blockDto.BlockHash}, Transaction count:{blockDto.Body.TransactionsCount}");
            var forked = false;

            Parallel.ForEach(collection.Skip(0), item =>
            {
                var (item1, item2) = item;
                if (item1 == null || item2 == null)
                {
                    Logger.Error($"Node height {height} request return null response.");
                    return;
                }

                if (item2.BlockHash == blockDto.BlockHash) return;
                forked = true;
                Logger.Info(
                    $"Node: {item1.GetServiceUrl()}, Block hash: {item2.BlockHash}, Transaction count:{item2.Body.TransactionsCount}");
            });

            if (forked)
                Logger.Error($"Node forked at height: {height}");
        }
    }
}