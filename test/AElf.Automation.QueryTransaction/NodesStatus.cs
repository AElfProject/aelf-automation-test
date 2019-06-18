using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi;
using AElf.Automation.Common.WebApi.Dto;
using Nito.AsyncEx;

namespace AElf.Automation.QueryTransaction
{
    public class NodesStatus
    {
        private static readonly ILogHelper Logger = LogHelper.GetLogHelper();
        private readonly List<WebApiService> _apiServices;
        private long _height = 1;

        public NodesStatus(IEnumerable<string> urls)
        {
            _apiServices = new List<WebApiService>();
            foreach (var url in urls)
            {
                var apiHelper = new WebApiService(url);
                _apiServices.Add(apiHelper);
            }
        }

        public async Task CheckAllNodes()
        {
            while (true)
            {
                var currentHeight = await _apiServices.First().GetBlockHeight();
                if (currentHeight == _height)
                {
                    Thread.Sleep(1000);
                }

                for (var i = _height; i < currentHeight; i++)
                {
                    await CheckNodeHeight(i);
                }

                _height = currentHeight;
            }
        }

        private async Task CheckNodeHeight(long height)
        {
            var collection = new List<(WebApiService, BlockDto)>();
            await _apiServices.AsParallel().Select(async api =>
            {
                var block = await api.GetBlockByHeight(height);
                collection.Add((api, block));
                return collection;
            }).WhenAll();

            var (webApiService, blockDto) = collection.First();
            Logger.WriteInfo($"Check height: {height}");
            Logger.WriteInfo(
                $"Node: {webApiService.BaseUrl}, Block hash: {blockDto.BlockHash}, Transaction count:{blockDto.Body.TransactionsCount}");
            var forked = false;

            Parallel.ForEach(collection.Skip(0), item =>
            {
                var (item1, item2) = item;
                if (item2.BlockHash == blockDto.BlockHash) return;
                forked = true;
                Logger.WriteInfo(
                    $"Node: {item1.BaseUrl}, Block hash: {item2.BlockHash}, Transaction count:{item2.Body.TransactionsCount}");
            });

            if (forked)
                Logger.WriteError($"Node forked at height: {height}");
        }
    }
}