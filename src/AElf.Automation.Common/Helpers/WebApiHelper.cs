using System.Collections.Generic;
using Acs0;
using AElf.Automation.Common.OptionManagers;
using AElf.Automation.Common.WebApi;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using Newtonsoft.Json.Linq;
using Volo.Abp.Threading;

namespace AElf.Automation.Common.Helpers
{
    public class WebApiHelper : IApiHelper
    {
        #region Properties

        private string _baseUrl;
        private string _chainId;
        private readonly AElfKeyStore _keyStore;
        private readonly ILogHelper _logger = LogHelper.GetLogHelper();
        private string _genesisAddress;
        private Dictionary<ApiMethods, string> ApiRoute { get; set; }

        public AccountManager AccountManager { get; set; }
        public TransactionManager TransactionManager { get; set; }

        #endregion

        public WebApiHelper(string baseUrl, string keyPath = "")
        {
            _baseUrl = baseUrl;
            _keyStore = new AElfKeyStore(keyPath == "" ? CommonHelper.GetDefaultDataDir() : keyPath);

            ApiService = new WebApiService(baseUrl);
            CommandList = new List<CommandInfo>();

            InitializeWebApiRoute();
        }

        public string GetApiUrl()
        {
            return _baseUrl;
        }

        public void UpdateApiUrl(string url)
        {
            _baseUrl = url;
            _logger.WriteInfo($"Request url updated to: {url}");
        }

        public WebApiService ApiService { get; set; }

        public List<CommandInfo> CommandList { get; set; }

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
                case ApiMethods.SendTransaction:
                    BroadcastTx(ci);
                    break;
                case ApiMethods.SendTransactions:
                    BroadcastTxs(ci);
                    break;
                case ApiMethods.GetTransactionResult:
                    GetTransactionResult(ci);
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

            if (!ci.Result) //analyze failed result
                CommandList.Add(ci);

            return ci;
        }

        #region Account methods

        public CommandInfo NewAccount(CommandInfo ci)
        {
            if (AccountManager == null)
                AccountManager = new AccountManager(_keyStore, "AELF");
            ci = AccountManager.NewAccount(ci.Parameter);
            return ci;
        }

        public CommandInfo ListAccounts()
        {
            if (AccountManager == null)
                AccountManager = new AccountManager(_keyStore, "AELF");
            var ci = AccountManager.ListAccount();
            return ci;
        }

        public CommandInfo UnlockAccount(CommandInfo ci)
        {
            if (AccountManager == null)
                AccountManager = new AccountManager(_keyStore, "AELF");
            var parameters = ci.Parameter.Split(" ");
            ci = AccountManager.UnlockAccount(parameters[0], parameters[1],
                parameters[2]);
            return ci;
        }

        #endregion

        #region Web request methods

        public void GetChainInformation(CommandInfo ci)
        {
            var statusDto = ApiService.GetChainStatus().Result;

            _genesisAddress = statusDto.GenesisContractAddress;

            _chainId = statusDto.ChainId;
            TransactionManager = new TransactionManager(_keyStore, _chainId);
            AccountManager = new AccountManager(_keyStore, _chainId);

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

            TransactionManager.SetCmdInfo(ci);
            var tx = TransactionManager.CreateTransaction(from, _genesisAddress,
                ci.Cmd, input.ToByteString());
            tx = tx.AddBlockReference(_baseUrl, _chainId);

            if (tx == null)
                return;
            tx = TransactionManager.SignTransaction(tx);
            if (tx == null)
                return;
            var rawTxString = TransactionManager.ConvertTransactionRawTxString(tx);

            var transactionOutput = AsyncHelper.RunSync(() => ApiService.SendTransaction(rawTxString));

            ci.InfoMsg = transactionOutput;
            ci.Result = true;
        }

        public void BroadcastTx(CommandInfo ci)
        {
            var tr = TransactionManager.ConvertFromCommandInfo(ci);

            var parameter = ci.ParameterInput.ToByteString();
            tr.Params = parameter == null ? ByteString.Empty : parameter;
            tr = tr.AddBlockReference(_baseUrl, _chainId);
            TransactionManager.SignTransaction(tr);

            var rawTxString = TransactionManager.ConvertTransactionRawTxString(tr);

            ci.InfoMsg = AsyncHelper.RunSync(() => ApiService.SendTransaction(rawTxString));
            ci.Result = true;
        }

        public void BroadcastWithRawTx(CommandInfo ci)
        {
            if (!ci.CheckParameterValid(1))
                return;
            ci.InfoMsg = AsyncHelper.RunSync(() => ApiService.SendTransaction(ci.Parameter));
            ci.Result = true;
        }

        public string GenerateTransactionRawTx(CommandInfo ci)
        {
            var tr = TransactionManager.ConvertFromCommandInfo(ci);

            if (tr.MethodName == null)
            {
                ci.ErrorMsg = "Method not found.";
                return string.Empty;
            }

            var parameter = ci.ParameterInput.ToByteString();
            tr.Params = parameter == null ? ByteString.Empty : parameter;
            tr = tr.AddBlockReference(_baseUrl, _chainId);

            TransactionManager.SignTransaction(tr);

            return tr.ToByteArray().ToHex();
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
            tr = tr.AddBlockReference(_baseUrl, _chainId);

            TransactionManager.SignTransaction(tr);

            return tr.ToByteArray().ToHex();
        }

        public void BroadcastTxs(CommandInfo ci)
        {
            if (!ci.CheckParameterValid(1))
                return;

            ci.InfoMsg = AsyncHelper.RunSync(() => ApiService.SendTransactions(ci.Parameter));
            ci.Result = true;
        }

        public void GetTransactionResult(CommandInfo ci)
        {
            if (!ci.CheckParameterValid(1))
                return;

            ci.InfoMsg = AsyncHelper.RunSync(() => ApiService.GetTransactionResult(ci.Parameter));
            ci.Result = true;
        }

        public void GetBlockHeight(CommandInfo ci)
        {
            ci.InfoMsg = AsyncHelper.RunSync(() => ApiService.GetBlockHeight());
            ci.Result = true;
        }

        public void GetBlockByHeight(CommandInfo ci)
        {
            if (!ci.CheckParameterValid(2))
                return;

            var parameterArray = ci.Parameter.Split(" ");
            ci.InfoMsg = AsyncHelper.RunSync(
                () => ApiService.GetBlockByHeight(long.Parse(parameterArray[0]), bool.Parse(parameterArray[1]))
            );
            ci.Result = true;
        }

        public void GetBlockByHash(CommandInfo ci)
        {
            if (!ci.CheckParameterValid(2))
                return;

            var parameterArray = ci.Parameter.Split(" ");
            ci.InfoMsg =
                AsyncHelper.RunSync(() => ApiService.GetBlock(parameterArray[0], bool.Parse(parameterArray[1])));
            ci.Result = true;
        }

        public void GetTransactionPoolStatus(CommandInfo ci)
        {
            ci.InfoMsg = AsyncHelper.RunSync(() => ApiService.GetTransactionPoolStatus());
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
            transaction = TransactionManager.SignTransaction(transaction);

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
            transaction = TransactionManager.SignTransaction(transaction);

            var resp = CallTransaction(transaction);

            //deserialize response
            if (resp == null)
            {
                _logger.WriteError($"ExecuteTransaction response is null or empty.");
                return default(T);
            }

            var byteArray = ByteArrayHelpers.FromHexString(resp);
            var messageParser = new MessageParser<T>(() => new T());

            return messageParser.ParseFrom(byteArray);
        }

        public void QueryViewInfo(CommandInfo ci)
        {
            ci.InfoMsg = AsyncHelper.RunSync(() => ApiService.ExecuteTransaction(ci.Parameter));
            ci.Result = true;
        }

        public string GetPublicKeyFromAddress(string account, string password = "123")
        {
            if (AccountManager == null)
                AccountManager = new AccountManager(_keyStore, "AELF");
            AccountManager.UnlockAccount(account, password, "notimeout");
            return AccountManager.GetPublicKey(account);
        }

        //Net Api
        public void NetGetPeers(CommandInfo ci)
        {
            ci.InfoMsg = AsyncHelper.RunSync(() => ApiService.GetPeers());
            ci.Result = true;
        }

        public void NetAddPeer(CommandInfo ci)
        {
            if (!ci.CheckParameterValid(1))
                return;

            ci.InfoMsg = AsyncHelper.RunSync(() => ApiService.AddPeer(ci.Parameter));
            ci.Result = true;
        }

        public void NetRemovePeer(CommandInfo ci)
        {
            if (!ci.CheckParameterValid(1))
                return;

            ci.InfoMsg = AsyncHelper.RunSync(() => ApiService.RemovePeer(ci.Parameter));
            ci.Result = true;
        }

        #endregion

        private string CallTransaction(Transaction tx)
        {
            var rawTxString = TransactionManager.ConvertTransactionRawTxString(tx);
            return AsyncHelper.RunSync(() => ApiService.ExecuteTransaction(rawTxString));
        }

        private void InitializeWebApiRoute()
        {
            ApiRoute = new Dictionary<ApiMethods, string>();

            //chain route
            ApiRoute.Add(ApiMethods.GetChainInformation, "/api/blockChain/chainStatus");
            ApiRoute.Add(ApiMethods.GetBlockHeight, "/api/blockChain/blockHeight");
            ApiRoute.Add(ApiMethods.GetBlockByHeight,
                "/api/blockChain/blockByHeight?blockHeight={0}&includeTransactions={1}");
            ApiRoute.Add(ApiMethods.GetBlockByHash, "/api/blockChain/block?blockHash={0}&includeTransactions={1}");
            ApiRoute.Add(ApiMethods.DeploySmartContract, "/api/blockChain/sendTransaction");
            ApiRoute.Add(ApiMethods.SendTransaction, "/api/blockChain/sendTransaction");
            ApiRoute.Add(ApiMethods.SendTransactions, "/api/blockChain/sendTransactions");
            ApiRoute.Add(ApiMethods.QueryView, "/api/blockChain/executeTransaction");
            ApiRoute.Add(ApiMethods.GetTransactionResult, "/api/blockChain/transactionResult?transactionId={0}");
            ApiRoute.Add(ApiMethods.GetTransactionResults,
                "/api/blockChain/transactionResults?blockHash={0}&offset={1}&limit={2}");

            //net route
            ApiRoute.Add(ApiMethods.GetPeers, "/api/net/peers");
            ApiRoute.Add(ApiMethods.AddPeer, "/api/net/peer");
            ApiRoute.Add(ApiMethods.RemovePeer, "/api/net/peer?address={0}");
        }
    }
}