using System;
using System.Collections.Generic;
using AElf.Automation.Common.Extensions;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.WebApp.Application.Chain.Dto;
using Google.Protobuf;
using Newtonsoft.Json.Linq;

namespace AElf.Automation.Common.Helpers
{
    public class WebApiHelper : IApiHelper
    {
        #region Properties

        private readonly string _rpcAddress;
        private string _chainId;
        private readonly AElfKeyStore _keyStore;
        private AccountManager _accountManager;
        private TransactionManager _transactionManager;
        private readonly WebRequestManager _requestManager;
        private readonly ILogHelper _logger = LogHelper.GetLogHelper();
        private List<CommandInfo> CommandList { get; }
        
        public string GenesisAddress;
        public Dictionary<ApiMethods, string> ApiRoute { get; set; }

        #endregion

        public WebApiHelper(string rpcUrl, string keyPath = "")
        {
            _rpcAddress = rpcUrl.Replace("/chain", "");
            _keyStore = new AElfKeyStore(keyPath == "" ? ApplicationHelper.GetDefaultDataDir() : keyPath);
            _requestManager = new WebRequestManager(rpcUrl);

            CommandList = new List<CommandInfo>();
            InitializeWebApiRoute();
        }

        public ApiType GetApiType()
        {
            return ApiType.WebApi;
        }

        public string GetGenesisContractAddress()
        {
            return GenesisAddress;
        }

        public CommandInfo ExecuteCommand(CommandInfo ci)
        {
            switch (ci.Cmd)
            {
                //Account request
                case "AccountNew":
                    ci = NewAccount(ci);
                    break;
                case "AccountList":
                    ci = ListAccounts();
                    break;
                case "AccountUnlock":
                    ci = UnlockAccount(ci);
                    break;
                case "GetChainInformation":
                    RpcGetChainInformation(ci);
                    break;
                case "DeploySmartContract":
                    RpcDeployContract(ci);
                    break;
                case "BroadcastTransaction":
                    RpcBroadcastTx(ci);
                    break;
                case "BroadcastTransactions":
                    RpcBroadcastTxs(ci);
                    break;
                case "GetTransactionResult":
                    RpcGetTxResult(ci);
                    break;
                case "GetBlockHeight":
                    RpcGetBlockHeight(ci);
                    break;
                case "GetBlockByHeight":
                    RpcGetBlockByHeight(ci);
                    break;
                case "GetBlockByHash":
                    RpcGetBlockByHash(ci);
                    break;
                case "QueryView":
                    RpcQueryViewInfo(ci);
                    break;
                default:
                    _logger.WriteError("Invalid command.");
                    break;
            }

            ci.PrintResultMessage();
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

        public void RpcGetChainInformation(CommandInfo ci)
        {
            var url = "/api/blockChain/chainStatus";            
            var statusDto = _requestManager.GetResponse<ChainStatusDto>(url, out var returnCode, out var timeSpan);
            ci.TimeSpan = timeSpan;
            if (!CheckResponse(ci, returnCode, statusDto?.GenesisContractAddress))
                return;

            GenesisAddress = statusDto?.GenesisContractAddress;
            
            _chainId = statusDto?.ChainId;
            _transactionManager = new TransactionManager(_keyStore, _chainId);
            _accountManager = new AccountManager(_keyStore, _chainId);
            
            var message = statusDto?.ToString();
            ci.InfoMsg.Add(message);
            ci.Result = true;
        }

        public void RpcDeployContract(CommandInfo ci)
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
            var tx = _transactionManager.CreateTransaction(from, GenesisAddress,
                ci.Cmd, input.ToByteString());
            tx = tx.AddBlockReference(_rpcAddress);
            if (tx == null)
                return;
            tx = _transactionManager.SignTransaction(tx);
            if (tx == null)
                return;
            var rawTxString = _transactionManager.ConvertTransactionRawTxString(tx);
            
            var parameters = new Dictionary<string, string>()
            {
                { "rawTransaction", rawTxString }
            };
            var url = "/api/blockChain/broadcastTransaction";
            var resp = _requestManager.PostResponse<BroadcastTransactionOutput>(url, parameters, out var returnCode, out var timeSpan);
            ci.TimeSpan = timeSpan;
            if (!CheckResponse(ci, returnCode, resp?.TransactionId))
            {
                ci.Result = false;
                ci.ErrorMsg.Add(resp?.ToString());
                return;
            }

            ci.InfoMsg.Add(resp?.TransactionId);

            ci.Result = true;
        }

        public void RpcBroadcastTx(CommandInfo ci)
        {
            JObject j = null;
            if (ci.Parameter != null)
            {
                if (!ci.Parameter.Contains("{"))
                {
                    RpcBroadcastWithRawTx(ci);
                    return;
                }

                j = JObject.Parse(ci.Parameter);
            }

            var tr = _transactionManager.ConvertFromJson(j) ?? _transactionManager.ConvertFromCommandInfo(ci);

            var parameter = ci.ParameterInput.ToByteString();
            tr.Params = parameter == null ? ByteString.Empty : parameter;

            tr = tr.AddBlockReference(_rpcAddress);

            _transactionManager.SignTransaction(tr);
            
            var rawTxString = _transactionManager.ConvertTransactionRawTxString(tr);
            var parameters = new Dictionary<string, string>
            {
                { "rawTransaction", rawTxString }
            };
            var url = "/api/blockChain/broadcastTransaction";

            var resp = _requestManager.PostResponse<BroadcastTransactionOutput>(url, parameters, out var returnCode, out var timeSpan);
            ci.TimeSpan = timeSpan;
            if (!CheckResponse(ci, returnCode, resp?.TransactionId))
                return;

            string hash = resp?.TransactionId;
            var jobj = new JObject
            {
                ["TransactionId"] = hash
            };
            ci.InfoMsg.Add(jobj.ToString());
            ci.Result = true;
        }

        public void RpcBroadcastWithRawTx(CommandInfo ci)
        {
            var parameter = new Dictionary<string, string>
            {
                { "rawTransaction", ci.Parameter }
            };
            var url = "/api/blockChain/broadcastTransaction";

            var resp = _requestManager.PostResponse<BroadcastTransactionOutput>(url, parameter, out var returnCode, out var timeSpan);
            ci.TimeSpan = timeSpan;
            if (!CheckResponse(ci, returnCode, resp?.TransactionId))
                return;

            
            var jobj = new JObject
            {
                ["TransactionId"] = resp?.TransactionId
            };
            ci.InfoMsg.Add(jobj.ToString());

            ci.Result = true;
        }

        public string RpcGenerateTransactionRawTx(CommandInfo ci)
        {
            JObject j = null;
            if (ci.Parameter != null)
                j = JObject.Parse(ci.Parameter);
            var tr = _transactionManager.ConvertFromJson(j) ?? _transactionManager.ConvertFromCommandInfo(ci);

            if (tr.MethodName == null)
            {
                ci.ErrorMsg.Add("Method not found.");
                return string.Empty;
            }

            var parameter = ci.ParameterInput.ToByteString();
            tr.Params = parameter == null ? ByteString.Empty : parameter;
            tr = tr.AddBlockReference(_rpcAddress);

            _transactionManager.SignTransaction(tr);
            var rawtx = _transactionManager.ConvertTransactionRawTx(tr);

            return rawtx["rawTransaction"].ToString();
        }

        public string RpcGenerateTransactionRawTx(string from, string to, string methodName, IMessage inputParameter)
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
            tr = tr.AddBlockReference(_rpcAddress);

            _transactionManager.SignTransaction(tr);
            var rawTx = _transactionManager.ConvertTransactionRawTx(tr);

            return rawTx["rawTransaction"].ToString();
        }

        public void RpcBroadcastTxs(CommandInfo ci)
        {
            var parameters = new Dictionary<string, string>
            {
                { "rawTransactions", ci.Parameter }
            };
            var url = "/api/blockChain/broadcastTransactions";
            var resp = _requestManager.PostResponse<string[]>(url, parameters, out var returnCode, out var timeSpan);
            ci.TimeSpan = timeSpan;
            if (!CheckResponse(ci, returnCode, resp.ToString()))
                return;

            ci.InfoMsg.Add(resp.JoinAsString(","));
            ci.Result = true;
        }

        public void RpcGetCommands(CommandInfo ci)
        {
            throw new NotImplementedException();
        }

        public void RpcGetTxResult(CommandInfo ci)
        {
            if (!ci.CheckParameterValid(1))
                return;

            var url = $"/api/blockChain/transactionResult?transactionId={ci.Parameter}";
            var respDto = _requestManager.GetResponse<TransactionResultDto>(url, out var returnCode, out var timeSpan);
            ci.TimeSpan = timeSpan;
            if (!CheckResponse(ci, returnCode, respDto?.TransactionId))
                return;
            ci.InfoMsg.Add(respDto);
            ci.Result = true;
        }

        public void RpcGetBlockHeight(CommandInfo ci)
        {
            var url = "/api/blockChain/blockHeight";
            var resp = _requestManager.GetResponse<long>(url, out var returnCode, out var timeSpan);
            ci.TimeSpan = timeSpan;
            if (!CheckResponse(ci, returnCode, resp.ToString()))
                return;
            ci.InfoMsg.Add(resp.ToString());
            ci.Result = true;
        }

        public void RpcGetBlockInfo(CommandInfo ci)
        {
            throw new NotImplementedException();
        }

        public void RpcGetBlockByHeight(CommandInfo ci)
        {
            if (!ci.CheckParameterValid(2))
                return;

            var parameterArray = ci.Parameter.Split(" ");
            var url = $"/api/blockChain/blockByHeight?blockHeight={parameterArray[0]}&includeTransactions={parameterArray[1]}";
            var resp = _requestManager.GetResponse<BlockDto>(url, out var returnCode, out var timeSpan);
            ci.TimeSpan = timeSpan;
            if (!CheckResponse(ci, returnCode, resp?.BlockHash))
                return;
            ci.InfoMsg.Add(resp?.ToString());
            ci.Result = true;
        }
        
        public void RpcGetBlockByHash(CommandInfo ci)
        {
            if (!ci.CheckParameterValid(2))
                return;
            
            var parameterArray = ci.Parameter.Split(" ");
            var url = $"/api/blockChain/block?blockHash={parameterArray[0]}&includeTransactions={parameterArray[1]}";
            var resp = _requestManager.GetResponse<BlockDto>(url, out var returnCode, out var timeSpan);
            ci.TimeSpan = timeSpan;
            if (!CheckResponse(ci, returnCode, resp?.BlockHash))
                return;
            ci.InfoMsg.Add(resp?.ToString());
            ci.Result = true;
        }

        public void RpcGetMerklePath(CommandInfo ci)
        {
            throw new NotImplementedException();
        }

        public JObject RpcQueryView(string from, string to, string methodName, IMessage inputParameter)
        {
            var transaction = new Transaction()
            {
                From = Address.Parse(from),
                To = Address.Parse(to),
                MethodName = methodName,
                Params = inputParameter == null ? ByteString.Empty : inputParameter.ToByteString()
            };
            transaction = _transactionManager.SignTransaction(transaction);

            var resp = CallTransaction(transaction, "call");

            if (resp == string.Empty)
                return new JObject();

            return JObject.Parse(resp);
        }

        public T RpcQueryView<T>(string from, string to, string methodName, IMessage inputParameter)
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

            var resp = CallTransaction(transaction, "call");

            //deserialize response
            if(resp == null)
            {
                _logger.WriteError($"Call response is null or empty.");
                return default(T);
            }

            var byteArray = ByteArrayHelpers.FromHexString(resp.ToString());
            var messageParser = new MessageParser<T>(() => new T());

            return messageParser.ParseFrom(byteArray);
        }

        public void RpcQueryViewInfo(CommandInfo ci)
        {
            var parameters = new Dictionary<string, string>
            {
                { "rawTransaction", ci.Parameter }
            };
            var url = "/api/blockChain/call";
            string resp = _requestManager.PostResponse<string>(url, parameters, out var returnCode, out var timeSpan);
            ci.TimeSpan = timeSpan;
            if (!CheckResponse(ci, returnCode, resp))
                return;

            ci.InfoMsg.Add(resp);

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
            var url = "api/net/peers";
            var resp = _requestManager.GetResponse<List<string>>(url, out var returnCode, out var timeSpan);
            ci.TimeSpan = timeSpan;
            if (!CheckResponse(ci, returnCode, resp.ToString()))
                return;

            ci.InfoMsg.Add(resp.ToString());
            ci.Result = true;
        }

        public void NetAddPeer(CommandInfo ci)
        {
            var parameters = new Dictionary<string, string>
            {
                { "address", ci.Parameter }
            };
            var url = "/api/net/peer";
            var resp = _requestManager.PostResponse<bool>(url, parameters, out var returnCode, out var timeSpan);
            ci.TimeSpan = timeSpan;
            if (!CheckResponse(ci, returnCode, resp.ToString()))
                return;

            ci.InfoMsg.Add(resp.ToString());
            ci.Result = true;
        }

        public void NetRemovePeer(CommandInfo ci)
        {
            var url = $"/api/net/peer?address={ci.Parameter}";
            var resp = _requestManager.DeleteResponse<bool>(url, out var returnCode, out var timeSpan);
            ci.TimeSpan = timeSpan;
            if (!CheckResponse(ci, returnCode, resp.ToString()))
                return;

            ci.InfoMsg.Add(resp.ToString());
            ci.Result = true;
        }

        #endregion

        private string CallTransaction(Transaction tx, string api)
        {
            var rawTxString = _transactionManager.ConvertTransactionRawTxString(tx);
            var parameters = new Dictionary<string, string>
            {
                { "rawTransaction", rawTxString }
            };
            var url = "/api/blockChain/call";

            var resp = _requestManager.PostResponse<string>(url, parameters, out var returnCode, out var timeSpan);

            return resp;
        }

        private bool CheckResponse(CommandInfo ci, string returnCode, string response)
        {
            if (response == null)
            {
                ci.ErrorMsg.Add("Could not connect to server.");
                return false;
            }

            if (returnCode != "OK")
            {
                ci.ErrorMsg.Add("Http request failed, status: " + returnCode);
                return false;
            }

            if (!response.IsNullOrEmpty()) return true;
            
            ci.ErrorMsg.Add("Failed. Pleas check input.");
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