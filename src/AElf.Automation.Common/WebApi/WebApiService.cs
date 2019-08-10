using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.WebApi.Dto;
using Google.Protobuf;

namespace AElf.Automation.Common.WebApi
{
    public class WebApiService : IApiService
    {
        public string BaseUrl { get; }
        private Dictionary<ApiMethods, string> _apiRoute;
        private readonly ILog _logger = LogHelper.GetLogHelper();

        public WebApiService(string baseUrl)
        {
            BaseUrl = baseUrl;

            InitializeWebApiRoute();
        }

        #region Chain Api

        public async Task<string> ExecuteTransaction(string rawTransaction)
        {
            var url = GetRequestUrl(ApiMethods.ExecuteTransaction);
            var parameters = new Dictionary<string, string>
            {
                {"rawTransaction", rawTransaction}
            };
            return await HttpHelper.PostResponseAsync<string>(url, parameters);
        }

        public async Task<TResult> ExecuteTransaction<TResult>(string rawTransaction)
            where TResult : IMessage<TResult>, new()
        {
            var hexString = await ExecuteTransaction(rawTransaction);

            if (hexString.IsNullOrEmpty())
            {
                _logger.Error("ExecuteTransaction response is null or empty.");
                return default(TResult);
            }

            var byteArray = ByteArrayHelper.HexStringToByteArray(hexString);
            var messageParser = new MessageParser<TResult>(() => new TResult());

            return messageParser.ParseFrom(byteArray);
        }

        public async Task<byte[]> GetContractFileDescriptorSet(string address)
        {
            var url = GetRequestUrl(ApiMethods.GetContractFileDescriptorSet, address);

            return await HttpHelper.GetResponseAsync<byte[]>(url);
        }

        public async Task<CreateRawTransactionOutput> CreateRawTransaction(CreateRawTransactionInput input)
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
            return await HttpHelper.PostResponseAsync<CreateRawTransactionOutput>(url, parameters);
        }

        public async Task<SendRawTransactionOutput> SendRawTransaction(SendRawTransactionInput input)
        {
            var url = GetRequestUrl(ApiMethods.SendRawTransaction);
            var parameters = new Dictionary<string, string>
            {
                {"Transaction", input.Transaction},
                {"Signature", input.Signature},
                {"ReturnTransaction", input.ReturnTransaction ? "true" : "false"}
            };
            return await HttpHelper.PostResponseAsync<SendRawTransactionOutput>(url, parameters);
        }

        public async Task<SendTransactionOutput> SendTransaction(string rawTransaction)
        {
            var url = GetRequestUrl(ApiMethods.SendTransaction);
            var parameters = new Dictionary<string, string>
            {
                {"rawTransaction", rawTransaction}
            };
            return await HttpHelper.PostResponseAsync<SendTransactionOutput>(url, parameters);
        }

        public async Task<string[]> SendTransactions(string rawTransactions)
        {
            var url = GetRequestUrl(ApiMethods.SendTransactions);
            var parameters = new Dictionary<string, string>
            {
                {"rawTransactions", rawTransactions}
            };
            return await HttpHelper.PostResponseAsync<string[]>(url, parameters);
        }

        public async Task<TransactionResultDto> GetTransactionResult(string transactionId)
        {
            var url = GetRequestUrl(ApiMethods.GetTransactionResult, transactionId);
            return await HttpHelper.GetResponseAsync<TransactionResultDto>(url);
        }

        public async Task<List<TransactionResultDto>> GetTransactionResults(string blockHash, int offset = 0,
            int limit = 10)
        {
            var url = GetRequestUrl(ApiMethods.GetTransactionResults, blockHash, offset, limit);
            return await HttpHelper.GetResponseAsync<List<TransactionResultDto>>(url);
        }

        public async Task<long> GetBlockHeight()
        {
            var url = GetRequestUrl(ApiMethods.GetBlockHeight);
            return await HttpHelper.GetResponseAsync<long>(url);
        }

        public async Task<BlockDto> GetBlock(string blockHash, bool includeTransactions = false)
        {
            var url = GetRequestUrl(ApiMethods.GetBlockByHash, blockHash, includeTransactions);
            return await HttpHelper.GetResponseAsync<BlockDto>(url);
        }

        public async Task<BlockDto> GetBlockByHeight(long blockHeight, bool includeTransactions = false)
        {
            var url = GetRequestUrl(ApiMethods.GetBlockByHeight, blockHeight, includeTransactions);
            return await HttpHelper.GetResponseAsync<BlockDto>(url);
        }

        public async Task<GetTransactionPoolStatusOutput> GetTransactionPoolStatus()
        {
            var url = GetRequestUrl(ApiMethods.GetTransactionPoolStatus);
            return await HttpHelper.GetResponseAsync<GetTransactionPoolStatusOutput>(url);
        }

        public async Task<ChainStatusDto> GetChainStatus()
        {
            var url = GetRequestUrl(ApiMethods.GetChainStatus);
            return await HttpHelper.GetResponseAsync<ChainStatusDto>(url);
        }

        public async Task<BlockStateDto> GetBlockState(string blockHash)
        {
            var url = GetRequestUrl(ApiMethods.GetBlockState, blockHash);
            return await HttpHelper.GetResponseAsync<BlockStateDto>(url);
        }

        public async Task<List<TaskQueueInfoDto>> GetTaskQueueStatus()
        {
            var url = GetRequestUrl(ApiMethods.TaskQueueStatus);
            return await HttpHelper.GetResponseAsync<List<TaskQueueInfoDto>>(url);
        }

        public async Task<RoundDto> GetCurrentRoundInformationAsync()
        {
            var url = GetRequestUrl(ApiMethods.CurrentRoundInformation);
            return await HttpHelper.GetResponseAsync<RoundDto>(url);
        }

        #endregion

        #region Net api

        public async Task<bool> AddPeer(string address)
        {
            var url = GetRequestUrl(ApiMethods.AddPeer);
            var parameters = new Dictionary<string, string>
            {
                {"address", address}
            };

            return await HttpHelper.PostResponseAsync<bool>(url, parameters);
        }

        public async Task<bool> RemovePeer(string address)
        {
            var url = GetRequestUrl(ApiMethods.RemovePeer, address);
            return await HttpHelper.DeleteResponseAsObjectAsync<bool>(url);
        }

        public async Task<List<PeerDto>> GetPeers()
        {
            var url = GetRequestUrl(ApiMethods.GetPeers);
            return await HttpHelper.GetResponseAsync<List<PeerDto>>(url);
        }

        #endregion

        #region Private methods

        private string GetRequestUrl(ApiMethods api, params object[] parameters)
        {
            var subUrl = string.Format(_apiRoute[api], parameters);

            return $"{BaseUrl}{subUrl}";
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

        #endregion
    }
}