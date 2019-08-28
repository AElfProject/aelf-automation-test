using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf;
using AElfChain.SDK.Models;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AElfChain.SDK
{
    public class ApiService : IApiService
    {
        private string _baseUrl;
        private readonly IHttpService _httpService;
        private Dictionary<ApiMethods, string> _apiRoute;

        public ILogger Logger { get; set; }

        public ApiService(SdkOption sdkOption)
        {
            _baseUrl = FormatServiceUrl(sdkOption.ServiceUrl);
            _httpService = new HttpService(sdkOption.TimeoutSeconds, sdkOption.FailReTryTimes);

            InitializeWebApiRoute();
        }
        
        public ApiService(IOptions<SdkOption> sdkOption, ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory.CreateLogger<ApiService>();
            _baseUrl = FormatServiceUrl(sdkOption.Value.ServiceUrl);
            _httpService = new HttpService(sdkOption.Value.TimeoutSeconds, sdkOption.Value.FailReTryTimes);

            InitializeWebApiRoute();
        }

        #region Chain Api

        public string GetServiceUrl()
        {
            return _baseUrl;
        }

        public void SetServiceUrl(string serviceUrl)
        {
            _baseUrl = FormatServiceUrl(serviceUrl);
            Logger.LogInformation($"Url updated to {_baseUrl}");
        }

        public async Task<string> ExecuteTransactionAsync(string rawTransaction)
        {
            var url = GetRequestUrl(ApiMethods.ExecuteTransaction);
            var parameters = new Dictionary<string, string>
            {
                {"RawTransaction", rawTransaction}
            };
            return await _httpService.PostResponseAsync<string>(url, parameters);
        }

        public async Task<string> ExecuteRawTransactionAsync(ExecuteRawTransactionDto input)
        {
            var url = GetRequestUrl(ApiMethods.ExecuteRawTransaction);
            var parameters = new Dictionary<string, string>
            {
                {"RawTransaction", input.RawTransaction},
                {"Signature", input.Signature}
            };

            return await _httpService.PostResponseAsync<string>(url, parameters);
        }

        public async Task<TResult> ExecuteTransactionAsync<TResult>(string rawTransaction)
            where TResult : IMessage<TResult>, new()
        {
            var hexString = await ExecuteTransactionAsync(rawTransaction);

            if (string.IsNullOrEmpty(hexString))
            {
                throw new AElfChainApiException("ExecuteTransactionAsync response is null or empty.");
            }

            var byteArray = ByteArrayHelper.HexStringToByteArray(hexString);
            var messageParser = new MessageParser<TResult>(() => new TResult());

            return messageParser.ParseFrom(byteArray);
        }

        public async Task<byte[]> GetContractFileDescriptorSetAsync(string address)
        {
            var url = GetRequestUrl(ApiMethods.GetContractFileDescriptorSet, address);

            return await _httpService.GetResponseAsync<byte[]>(url);
        }

        public async Task<CreateRawTransactionOutput> CreateRawTransactionAsync(CreateRawTransactionInput input)
        {
            var url = GetRequestUrl(ApiMethods.CreateRawTransaction);
            var parameters = new Dictionary<string, string>
            {
                {"From", input.From},
                {"To", input.To},
                {"RefBlockNumber", input.RefBlockNumber.ToString()},
                {"RefBlockHash", input.RefBlockHash},
                {"MethodName", input.MethodName},
                {"Params", input.Params}
            };
            return await _httpService.PostResponseAsync<CreateRawTransactionOutput>(url, parameters);
        }

        public async Task<SendRawTransactionOutput> SendRawTransactionAsync(SendRawTransactionInput input)
        {
            var url = GetRequestUrl(ApiMethods.SendRawTransaction);
            var parameters = new Dictionary<string, string>
            {
                {"Transaction", input.Transaction},
                {"Signature", input.Signature},
                {"ReturnTransaction", input.ReturnTransaction ? "true" : "false"}
            };
            return await _httpService.PostResponseAsync<SendRawTransactionOutput>(url, parameters);
        }

        public async Task<SendTransactionOutput> SendTransactionAsync(string rawTransaction)
        {
            var url = GetRequestUrl(ApiMethods.SendTransaction);
            var parameters = new Dictionary<string, string>
            {
                {"RawTransaction", rawTransaction}
            };
            return await _httpService.PostResponseAsync<SendTransactionOutput>(url, parameters);
        }

        public async Task<string[]> SendTransactionsAsync(string rawTransactions)
        {
            var url = GetRequestUrl(ApiMethods.SendTransactions);
            var parameters = new Dictionary<string, string>
            {
                {"RawTransactions", rawTransactions}
            };
            return await _httpService.PostResponseAsync<string[]>(url, parameters);
        }

        public async Task<TransactionResultDto> GetTransactionResultAsync(string transactionId)
        {
            var url = GetRequestUrl(ApiMethods.GetTransactionResult, transactionId);
            return await _httpService.GetResponseAsync<TransactionResultDto>(url);
        }

        public async Task<List<TransactionResultDto>> GetTransactionResultsAsync(string blockHash, int offset = 0,
            int limit = 10)
        {
            var url = GetRequestUrl(ApiMethods.GetTransactionResults, blockHash, offset, limit);
            return await _httpService.GetResponseAsync<List<TransactionResultDto>>(url);
        }

        public async Task<long> GetBlockHeightAsync()
        {
            var url = GetRequestUrl(ApiMethods.GetBlockHeight);
            return await _httpService.GetResponseAsync<long>(url);
        }

        public async Task<BlockDto> GetBlockAsync(string blockHash, bool includeTransactions = false)
        {
            var url = GetRequestUrl(ApiMethods.GetBlockByHash, blockHash, includeTransactions);
            return await _httpService.GetResponseAsync<BlockDto>(url);
        }

        public async Task<BlockDto> GetBlockByHeightAsync(long blockHeight, bool includeTransactions = false)
        {
            var url = GetRequestUrl(ApiMethods.GetBlockByHeight, blockHeight, includeTransactions);
            return await _httpService.GetResponseAsync<BlockDto>(url);
        }

        public async Task<GetTransactionPoolStatusOutput> GetTransactionPoolStatusAsync()
        {
            var url = GetRequestUrl(ApiMethods.GetTransactionPoolStatus);
            return await _httpService.GetResponseAsync<GetTransactionPoolStatusOutput>(url);
        }

        public async Task<ChainStatusDto> GetChainStatusAsync()
        {
            var url = GetRequestUrl(ApiMethods.GetChainStatus);
            return await _httpService.GetResponseAsync<ChainStatusDto>(url);
        }

        public async Task<BlockStateDto> GetBlockStateAsync(string blockHash)
        {
            var url = GetRequestUrl(ApiMethods.GetBlockState, blockHash);
            return await _httpService.GetResponseAsync<BlockStateDto>(url);
        }

        public async Task<List<TaskQueueInfoDto>> GetTaskQueueStatusAsync()
        {
            var url = GetRequestUrl(ApiMethods.TaskQueueStatus);
            return await _httpService.GetResponseAsync<List<TaskQueueInfoDto>>(url);
        }

        public async Task<RoundDto> GetCurrentRoundInformationAsync()
        {
            var url = GetRequestUrl(ApiMethods.CurrentRoundInformation);
            return await _httpService.GetResponseAsync<RoundDto>(url);
        }

        #endregion

        #region Net api

        public async Task<bool> AddPeerAsync(string address)
        {
            var url = GetRequestUrl(ApiMethods.AddPeer);
            var parameters = new Dictionary<string, string>
            {
                {"address", address}
            };

            return await _httpService.PostResponseAsync<bool>(url, parameters);
        }

        public async Task<bool> RemovePeerAsync(string address)
        {
            var url = GetRequestUrl(ApiMethods.RemovePeer, address);
            return await _httpService.DeleteResponseAsObjectAsync<bool>(url);
        }

        public async Task<List<PeerDto>> GetPeersAsync()
        {
            var url = GetRequestUrl(ApiMethods.GetPeers);
            return await _httpService.GetResponseAsync<List<PeerDto>>(url);
        }

        #endregion

        private string FormatServiceUrl(string serviceUrl)
        {
            if (serviceUrl == "")
                return serviceUrl;
            if (serviceUrl.Contains("http://") || serviceUrl.Contains("https://"))
                return serviceUrl;

            return $"http://{serviceUrl}";
        }

        private string GetRequestUrl(ApiMethods api, params object[] parameters)
        {
            if(_baseUrl == "")
                throw new ArgumentException("Service url should be set first.");
            var subUrl = string.Format(_apiRoute[api], parameters);

            return $"{_baseUrl}{subUrl}";
        }

        private void InitializeWebApiRoute()
        {
            _apiRoute = new Dictionary<ApiMethods, string>
            {
                //chain route
                {ApiMethods.GetChainInformation, "/api/blockChain/chainStatus"},
                {ApiMethods.GetChainStatus, "/api/blockChain/chainStatus"},
                {ApiMethods.GetBlockHeight, "/api/blockChain/blockHeight"},
                {ApiMethods.CreateRawTransaction, "/api/blockChain/rawTransaction"},
                {ApiMethods.GetTransactionPoolStatus, "/api/blockChain/transactionPoolStatus"},
                {ApiMethods.GetBlockByHeight, "/api/blockChain/blockByHeight?blockHeight={0}&includeTransactions={1}"},
                {ApiMethods.GetBlockByHash, "/api/blockChain/block?blockHash={0}&includeTransactions={1}"},
                {ApiMethods.DeploySmartContract, "/api/blockChain/sendTransaction"},
                {ApiMethods.SendTransaction, "/api/blockChain/sendTransaction"},
                {ApiMethods.SendTransactions, "/api/blockChain/sendTransactions"},
                {ApiMethods.SendRawTransaction, "/api/blockChain/sendRawTransaction"},
                {ApiMethods.GetBlockState, "/api/blockChain/blockState?blockHash={0}"},
                {ApiMethods.ExecuteTransaction, "/api/blockChain/executeTransaction"},
                {ApiMethods.ExecuteRawTransaction, "/api/blockChain/executeRawTransaction"},
                {ApiMethods.GetContractFileDescriptorSet, "/api/blockChain/contractFileDescriptorSet?address={0}"},
                {ApiMethods.GetTransactionResult, "/api/blockChain/transactionResult?transactionId={0}"},
                {
                    ApiMethods.GetTransactionResults,
                    "/api/blockChain/transactionResults?blockHash={0}&offset={1}&limit={2}"
                },
                {
                    ApiMethods.CurrentRoundInformation, "/api/blockChain/currentRoundInformation"
                },
                {ApiMethods.TaskQueueStatus, "/api/blockChain/taskQueueStatus"},

                //net route
                {ApiMethods.GetPeers, "/api/net/peers"},
                {ApiMethods.AddPeer, "/api/net/peer"},
                {ApiMethods.RemovePeer, "/api/net/peer?address={0}"}
            };
        }
    }
}