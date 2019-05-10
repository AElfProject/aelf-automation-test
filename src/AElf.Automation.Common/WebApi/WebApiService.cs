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
        private readonly string _baseUrl;
        private Dictionary<ApiMethods, string> _apiRoute;
        private readonly ILogHelper _logger = LogHelper.GetLogHelper();
        
        public WebApiService(string baseUrl)
        {
            _baseUrl = baseUrl;

            InitializeWebApiRoute();
        }
        
        public async Task<string> Call(string rawTransaction)
        {
            var url = GetRequestUrl(ApiMethods.Call);
            var parameters = new Dictionary<string, string>
            {
                { "rawTransaction", rawTransaction }
            };
            return await HttpHelper.PostResponseAsync<string>(url, parameters);
        }

        public async Task<TResult> Call<TResult>(string rawTransaction) where TResult : IMessage<TResult>, new()
        {
            var hexString = await Call(rawTransaction);
            
            if(hexString.IsNullOrEmpty())
            {
                _logger.WriteError($"Call response is null or empty.");
                return default(TResult);
            }

            var byteArray = ByteArrayHelpers.FromHexString(hexString);
            var messageParser = new MessageParser<TResult>(() => new TResult());

            return messageParser.ParseFrom(byteArray);
        }

        public async Task<byte[]> GetContractFileDescriptorSet(string address)
        {
            var url = GetRequestUrl(ApiMethods.GetContractFileDescriptorSet);
            var parameters = new Dictionary<string, string>
            {
                { "Address", address }
            };
            return await HttpHelper.PostResponseAsync<byte[]>(url, parameters);
        }

        public async Task<CreateRawTransactionOutput> CreateRawTransaction(CreateRawTransactionInput input)
        {
            var url = GetRequestUrl(ApiMethods.CreateRawTransaction);
            var parameters = new Dictionary<string,string>
            {
                { "From",input.From },
                { "To",input.To },
                { "RefBlockNumber", input.RefBlockNumber.ToString() },
                { "RefBlockHash", input.RefBlockHash },
                { "MethodName", input.MethodName },
                { "Params", input.Params }
            };
            return await HttpHelper.PostResponseAsync<CreateRawTransactionOutput>(url, parameters);
        }

        public async Task<SendRawTransactionOutput> SendRawTransaction(SendRawTransactionInput input)
        {
            var url = GetRequestUrl(ApiMethods.SendRawTransaction);
            var parameters = new Dictionary<string, string>
            {
                { "Transaction", input.Transaction },
                { "Signature", input.Signature },
                { "ReturnTransaction", input.ReturnTransaction ? "true" : "false" }
            };
            return await HttpHelper.PostResponseAsync<SendRawTransactionOutput>(url, parameters);
        }

        public async Task<BroadcastTransactionOutput> BroadcastTransaction(string rawTransaction)
        {
            var url = GetRequestUrl(ApiMethods.BroadcastTransaction);
            var parameters = new Dictionary<string,string>
            {
                {"rawTransaction",rawTransaction}
            };
            return await HttpHelper.PostResponseAsync<BroadcastTransactionOutput>(url, parameters);
        }

        public async Task<string[]> BroadcastTransactions(string rawTransactions)
        {
            var url = GetRequestUrl(ApiMethods.BroadcastTransactions);
            var parameters = new Dictionary<string,string>
            {
                {"rawTransactions",rawTransactions}
            };
            return await HttpHelper.PostResponseAsync<string[]>(url, parameters);
        }

        public async Task<TransactionResultDto> GetTransactionResult(string transactionId)
        {
            var url = GetRequestUrl(ApiMethods.GetTransactionResult, transactionId);
            return await HttpHelper.GetResponseAsync<TransactionResultDto>(url);
        }

        public async Task<List<TransactionResultDto>> GetTransactionResults(string blockHash, int offset = 0, int limit = 10)
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
        
        #region Net api

        public async Task<bool> AddPeer(string address)
        {
            var url = GetRequestUrl(ApiMethods.AddPeer);
            var parameters = new Dictionary<string, string>
            {
                { "address", address }
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

        private string GetRequestUrl(ApiMethods api, params object[] parameters)
        {
            var subUrl = string.Format(_apiRoute[api], parameters);

            return $"{_baseUrl}{subUrl}";
        }
        private void InitializeWebApiRoute()
        {
            _apiRoute = new Dictionary<ApiMethods, string>();
            
            //chain route
            _apiRoute.Add(ApiMethods.GetChainInformation, "/api/blockChain/chainStatus");
            _apiRoute.Add(ApiMethods.GetChainStatus, "/api/blockChain/chainStatus");
            _apiRoute.Add(ApiMethods.GetBlockHeight, "/api/blockChain/blockHeight");
            _apiRoute.Add(ApiMethods.CreateRawTransaction, "/api/blockChain/rawTransaction");
            _apiRoute.Add(ApiMethods.GetTransactionPoolStatus, "/api/blockChain/transactionPoolStatus");
            _apiRoute.Add(ApiMethods.GetBlockByHeight, "/api/blockChain/blockByHeight?blockHeight={0}&includeTransactions={1}");
            _apiRoute.Add(ApiMethods.GetBlockByHash, "/api/blockChain/block?blockHash={0}&includeTransactions={1}");
            _apiRoute.Add(ApiMethods.DeploySmartContract, "/api/blockChain/broadcastTransaction");
            _apiRoute.Add(ApiMethods.BroadcastTransaction, "/api/blockChain/broadcastTransaction");
            _apiRoute.Add(ApiMethods.BroadcastTransactions, "/api/blockChain/broadcastTransactions");
            _apiRoute.Add(ApiMethods.SendRawTransaction, "/api/blockChain/sendRawTransaction");
            _apiRoute.Add(ApiMethods.GetBlockState, "/api/blockChain/blockState?blockHash={0}");
            _apiRoute.Add(ApiMethods.Call, "/api/blockChain/call");
            _apiRoute.Add(ApiMethods.GetContractFileDescriptorSet, "/api/blockChain/contractFileDescriptorSet?address={0}");
            _apiRoute.Add(ApiMethods.GetTransactionResult, "/api/blockChain/transactionResult?transactionId={0}");
            _apiRoute.Add(ApiMethods.GetTransactionResults, "/api/blockChain/transactionResults?blockHash={0}&offset={1}&limit={2}");
            
            //net route
            _apiRoute.Add(ApiMethods.GetPeers, "/api/net/peers");
            _apiRoute.Add(ApiMethods.AddPeer, "/api/net/peer");
            _apiRoute.Add(ApiMethods.RemovePeer, "/api/net/peer?address={0}");
        }
    }
}