using System;
using System.Collections.Generic;
using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.WebApi;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Automation.Common.WebApi.Dto;
using Google.Protobuf;
using Newtonsoft.Json.Linq;

namespace AElf.Automation.Common.Helpers
{
    public class WebApiHelper : IApiHelper
    {
        #region Properties

        private readonly string _baseUrl;
        private string _chainId;
        private readonly AElfKeyStore _keyStore;
        private AccountManager _accountManager;
        private TransactionManager _transactionManager;
        private readonly WebRequestManager _requestManager;
        private readonly WebApiService _apiService;
        private readonly ILogHelper _logger = LogHelper.GetLogHelper();
        private string _genesisAddress;
        private List<CommandInfo> CommandList { get; }

        public Dictionary<ApiMethods, string> ApiRoute { get; set; }

        #endregion

        public WebApiHelper(string baseUrl, string keyPath = "")
        {
            _baseUrl = baseUrl;
            _requestManager = new WebRequestManager(baseUrl);
            _apiService = new WebApiService(baseUrl);
            _keyStore = new AElfKeyStore(keyPath == "" ? ApplicationHelper.GetDefaultDataDir() : keyPath);
            CommandList = new List<CommandInfo>();
            
            InitializeWebApiRoute();
        }

        public string GetGenesisContractAddress()
        {
            if (string.IsNullOrEmpty(_genesisAddress))
            {
                GetChainInformation(new CommandInfo
                {
                    Method = ApiMethods.GetChainInformation
                });
            }
            
            return _genesisAddress;
        }

        public CommandInfo ExecuteCommand(CommandInfo ci)
        {
            switch (ci.Method)
            {
                //Account request
                case ApiMethods.AccountNew:
                    ci = NewAccount(ci);
                    break;
                case ApiMethods.AccountList:
                    ci = ListAccounts();
                    break;
                case ApiMethods.AccountUnlock:
                    ci = UnlockAccount(ci);
                    break;
                case ApiMethods.GetChainInformation:
                    GetChainInformation(ci);
                    break;
                case ApiMethods.DeploySmartContract:
                    DeployContract(ci);
                    break;
                case ApiMethods.BroadcastTransaction:
                    BroadcastTx(ci);
                    break;
                case ApiMethods.BroadcastTransactions:
                    BroadcastTxs(ci);
                    break;
                case ApiMethods.GetTransactionResult:
                    GetTxResult(ci);
                    break;
                case ApiMethods.GetBlockHeight:
                    GetBlockHeight(ci);
                    break;
                case ApiMethods.GetBlockByHeight:
                    GetBlockByHeight(ci);
                    break;
                case ApiMethods.GetBlockByHash:
                    GetBlockByHash(ci);
                    break;
                case ApiMethods.QueryView:
                    QueryViewInfo(ci);
                    break;
                default:
                    _logger.WriteError("Invalid command.");
                    break;
            }

            ci.PrintResultMessage();
            
            if(!ci.Result)    //analyze failed result
                CommandList.Add(ci);

            return ci;
        }

        #region Account methods

        public CommandInfo NewAccount(CommandInfo ci)
        {
            ci = _accountManager.NewAccount(ci.Parameter);
            return ci;
        }

        public CommandInfo ListAccounts()
        {
            var ci = _accountManager.ListAccount();
            return ci;
        }

        public CommandInfo UnlockAccount(CommandInfo ci)
        {
            ci = _accountManager.UnlockAccount(ci.Parameter.Split(" ")?[0], ci.Parameter.Split(" ")?[1],
                ci.Parameter.Split(" ")?[2]);
            return ci;
        }

        #endregion

        #region Web request methods

        public void GetChainInformation(CommandInfo ci)
        {
            var statusDto = _apiService.GetChainStatus().Result;
            
            _genesisAddress = statusDto.GenesisContractAddress;
            
            _chainId = statusDto.ChainId;
            _transactionManager = new TransactionManager(_keyStore, _chainId);
            _accountManager = new AccountManager(_keyStore, _chainId);
            
            ci.InfoMsg = statusDto;
            ci.Result = true;
        }

        public void DeployContract(CommandInfo ci)
        {
            if (!ci.CheckParameterValid(2))
                return;
            var parameterArray = ci.Parameter.Split(" ");
            var filename = parameterArray[0];
            var from = parameterArray[1];

            // Read sc bytes
            var contractReader = new SmartContractReader();
            var codeArray = contractReader.Read(filename);
            var input = new ContractDeploymentInput
            {
                Category = KernelConstants.CodeCoverageRunnerCategory,
                Code = ByteString.CopyFrom(codeArray)
            };

            _transactionManager.SetCmdInfo(ci);
            var tx = _transactionManager.CreateTransaction(from, _genesisAddress,
                ci.Cmd, input.ToByteString());
            tx = tx.AddBlockReference(_baseUrl);
            if (tx == null)
                return;
            tx = _transactionManager.SignTransaction(tx);
            if (tx == null)
                return;
            var rawTxString = _transactionManager.ConvertTransactionRawTxString(tx);
            
            var transactionOutput = _apiService.BroadcastTransaction(rawTxString).Result;

            ci.InfoMsg = transactionOutput;
            ci.Result = true;
        }

        public void BroadcastTx(CommandInfo ci)
        {
            JObject j = null;
            if (ci.Parameter != null)
            {
                if (!ci.Parameter.Contains("{"))
                {
                    BroadcastWithRawTx(ci);
                    return;
                }

                j = JObject.Parse(ci.Parameter);
            }

            var tr = _transactionManager.ConvertFromJson(j) ?? _transactionManager.ConvertFromCommandInfo(ci);

            var parameter = ci.ParameterInput.ToByteString();
            tr.Params = parameter == null ? ByteString.Empty : parameter;

            tr = tr.AddBlockReference(_baseUrl);

            _transactionManager.SignTransaction(tr);
            
            var rawTxString = _transactionManager.ConvertTransactionRawTxString(tr);

            ci.InfoMsg = _apiService.BroadcastTransaction(rawTxString).Result;
            ci.Result = true;
        }

        public void BroadcastWithRawTx(CommandInfo ci)
        {
            if (!ci.CheckParameterValid(1))
                return;
        
            ci.InfoMsg = _apiService.BroadcastTransaction(ci.Parameter).Result;
            ci.Result = true;
        }

        public string GenerateTransactionRawTx(CommandInfo ci)
        {
            JObject j = null;
            if (ci.Parameter != null)
                j = JObject.Parse(ci.Parameter);
            var tr = _transactionManager.ConvertFromJson(j) ?? _transactionManager.ConvertFromCommandInfo(ci);

            if (tr.MethodName == null)
            {
                ci.ErrorMsg = "Method not found.";
                return string.Empty;
            }

            var parameter = ci.ParameterInput.ToByteString();
            tr.Params = parameter == null ? ByteString.Empty : parameter;
            tr = tr.AddBlockReference(_baseUrl);

            _transactionManager.SignTransaction(tr);
            var rawTx = _transactionManager.ConvertTransactionRawTx(tr);

            return rawTx["rawTransaction"].ToString();
        }

        public string GenerateTransactionRawTx(string from, string to, string methodName, IMessage inputParameter)
        {
            var tr = new Transaction()
            {
                From = Address.Parse(from),
                To = Address.Parse(to),
                MethodName = methodName
            };

            if (tr.MethodName == null)
            {
                _logger.WriteError("Method not found.");
                return string.Empty;
            }

            tr.Params = inputParameter == null ? ByteString.Empty : inputParameter.ToByteString();
            tr = tr.AddBlockReference(_baseUrl);

            _transactionManager.SignTransaction(tr);
            var rawTx = _transactionManager.ConvertTransactionRawTx(tr);

            return rawTx["rawTransaction"].ToString();
        }

        public void BroadcastTxs(CommandInfo ci)
        {
            if (!ci.CheckParameterValid(1))
                return;
            
            ci.InfoMsg = _apiService.BroadcastTransactions(ci.Parameter).Result;
            ci.Result = true;
        }

        public void GetTxResult(CommandInfo ci)
        {
            if (!ci.CheckParameterValid(1))
                return;

            ci.InfoMsg = _apiService.GetTransactionResult(ci.Parameter).Result;
            ci.Result = true;
        }

        public void GetBlockHeight(CommandInfo ci)
        {
            ci.InfoMsg = _apiService.GetBlockHeight().Result;    
            ci.Result = true;
        }

        public void GetBlockByHeight(CommandInfo ci)
        {
            if (!ci.CheckParameterValid(2))
                return;
            
            var parameterArray = ci.Parameter.Split(" ");
            ci.InfoMsg = _apiService.GetBlockByHeight(long.Parse(parameterArray[0]), bool.Parse(parameterArray[1])).Result;
            ci.Result = true;
        }
        
        public void GetBlockByHash(CommandInfo ci)
        {
            if (!ci.CheckParameterValid(2))
                return;
            
            var parameterArray = ci.Parameter.Split(" ");
            ci.InfoMsg = _apiService.GetBlock(parameterArray[0], bool.Parse(parameterArray[1])).Result;
            ci.Result = true;
        }

        public JObject QueryView(string from, string to, string methodName, IMessage inputParameter)
        {
            var transaction = new Transaction()
            {
                From = Address.Parse(from),
                To = Address.Parse(to),
                MethodName = methodName,
                Params = inputParameter == null ? ByteString.Empty : inputParameter.ToByteString()
            };
            transaction = _transactionManager.SignTransaction(transaction);

            var resp = CallTransaction(transaction);

            return resp == string.Empty ? new JObject() : JObject.Parse(resp);
        }

        public T QueryView<T>(string from, string to, string methodName, IMessage inputParameter)
            where T : IMessage<T>, new()
        {
            var transaction = new Transaction()
            {
                From = Address.Parse(from),
                To = Address.Parse(to),
                MethodName = methodName,
                Params = inputParameter == null ? ByteString.Empty : inputParameter.ToByteString()
            };
            transaction = _transactionManager.SignTransaction(transaction);

            var resp = CallTransaction(transaction);

            //deserialize response
            if(resp == null)
            {
                _logger.WriteError($"Call response is null or empty.");
                return default(T);
            }

            var byteArray = ByteArrayHelpers.FromHexString(resp);
            var messageParser = new MessageParser<T>(() => new T());

            return messageParser.ParseFrom(byteArray);
        }

        public void QueryViewInfo(CommandInfo ci)
        {
            ci.InfoMsg = _apiService.Call(ci.Parameter).Result;
            ci.Result = true;
        }

        public string GetPublicKeyFromAddress(string account, string password = "123")
        {
            _accountManager.UnlockAccount(account, password, "notimeout");
            return _accountManager.GetPublicKey(account);
        }

        //Net Api
        public void NetGetPeers(CommandInfo ci)
        {
            ci.InfoMsg = _apiService.GetPeers().Result;
            ci.Result = true;
        }

        public void NetAddPeer(CommandInfo ci)
        {
            if (!ci.CheckParameterValid(1))
                return;
            
            ci.InfoMsg = _apiService.AddPeer(ci.Parameter).Result;
            ci.Result = true;
        }

        public void NetRemovePeer(CommandInfo ci)
        {
            if (!ci.CheckParameterValid(1))
                return;

            ci.InfoMsg = _apiService.RemovePeer(ci.Parameter).Result;
            ci.Result = true;
        }

        #endregion

        private string CallTransaction(Transaction tx)
        {
            var rawTxString = _transactionManager.ConvertTransactionRawTxString(tx);
            return _apiService.Call(rawTxString).Result;
        }

        private bool CheckResponse(CommandInfo ci, string returnCode, string response)
        {
            if (response == null)
            {
                ci.ErrorMsg = "Could not connect to server.";
                return false;
            }

            if (returnCode != "OK")
            {
                ci.ErrorMsg = "Http request failed, status: " + returnCode;
                return false;
            }

            if (!response.IsNullOrEmpty()) return true;
            
            ci.ErrorMsg = "Failed. Pleas check input.";
            return false;

        }

        private void InitializeWebApiRoute()
        {
            ApiRoute = new Dictionary<ApiMethods, string>();
            
            //chain route
            ApiRoute.Add(ApiMethods.GetChainInformation, "/api/blockChain/chainStatus");
            ApiRoute.Add(ApiMethods.GetBlockHeight, "/api/blockChain/blockHeight");
            ApiRoute.Add(ApiMethods.GetBlockByHeight, "/api/blockChain/blockByHeight?blockHeight={0}&includeTransactions={1}");
            ApiRoute.Add(ApiMethods.GetBlockByHash, "/api/blockChain/block?blockHash={0}&includeTransactions={1}");
            ApiRoute.Add(ApiMethods.DeploySmartContract, "/api/blockChain/broadcastTransaction");
            ApiRoute.Add(ApiMethods.BroadcastTransaction, "/api/blockChain/broadcastTransaction");
            ApiRoute.Add(ApiMethods.BroadcastTransactions, "/api/blockChain/broadcastTransactions");
            ApiRoute.Add(ApiMethods.QueryView, "/api/blockChain/call");
            ApiRoute.Add(ApiMethods.GetTransactionResult, "/api/blockChain/transactionResult?transactionId={0}");
            ApiRoute.Add(ApiMethods.GetTransactionResults, "/api/blockChain/transactionResults?blockHash={0}&offset={1}&limit={2}");
            
            //net route
            ApiRoute.Add(ApiMethods.GetPeers, "api/net/peers");
            ApiRoute.Add(ApiMethods.AddPeer, "/api/net/peer");
            ApiRoute.Add(ApiMethods.RemovePeer, "/api/net/peer?address={0}");
        }
    }
}