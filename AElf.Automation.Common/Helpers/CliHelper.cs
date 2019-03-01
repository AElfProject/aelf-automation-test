using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.Automation.Common.Extensions;
using AElf.Cryptography;
using AElf.Common;
using Newtonsoft.Json.Linq;
using NServiceKit.Common;
using ProtoBuf;
using Module = AElf.Automation.Common.Protobuf.Module;
using Transaction = AElf.Automation.Common.Protobuf.Transaction;
using TransactionType = AElf.Automation.Common.Protobuf.TransactionType;
using Address = AElf.Automation.Common.Protobuf.Address;

namespace AElf.Automation.Common.Helpers
{
    public class CliHelper
    {
        #region Properties
        private string _rpcAddress;
        private string _genesisAddress;
        private string _chainId;
        private AElfKeyStore _keyStore;
        private AccountManager _accountManager;
        private TransactionManager _transactionManager;
        private RpcRequestManager _requestManager;
        
        private Dictionary<string, Module> _loadedModules;
        private readonly ILogHelper _logger = LogHelper.GetLogHelper();
        public List<CommandInfo> CommandList { get; }

        #endregion

        public CliHelper(string rpcUrl, string keyPath="")
        {
            _rpcAddress = rpcUrl;
            _keyStore = new AElfKeyStore(keyPath==""? ApplicationHelper.GetDefaultDataDir() : keyPath);
            _requestManager = new RpcRequestManager(rpcUrl);
            _loadedModules = new Dictionary<string, Module>();
            
            CommandList = new List<CommandInfo>(); 
        }

        public CommandInfo ExecuteCommand(CommandInfo ci)
        {
            switch (ci.Cmd)
            {
                //Account request
                case "account new":
                    ci = NewAccount(ci);
                    break;
                case "account list":
                    ci = ListAccounts(ci);
                    break;
                case "account unlock":
                    ci = UnlockAccount(ci);
                    break;
                case "ConnectChain":
                    RpcConnectChain(ci);
                    break;
                case "LoadContractAbi":
                    RpcLoadContractAbi(ci);
                    break;
                case "DeployContract":
                    RpcDeployContract(ci);
                    break;
                case "BroadcastTransaction":
                    RpcBroadcastTx(ci);
                    break;
                case "BroadcastTransactions":
                    RpcBroadcastTxs(ci);
                    break;
                case "GetCommands":
                    RpcGetCommands(ci);
                    break;
                case "GetContractAbi":
                    RpcGetContractAbi(ci);
                    break;
                case "GetIncrement":
                    RpcGetIncrement(ci);
                    break;
                case "GetTransactionResult":
                    RpcGetTxResult(ci);
                    break;
                case "GetBlockHeight":
                    RpcGetBlockHeight(ci);
                    break;
                case "GetBlockInfo":
                    RpcGetBlockInfo(ci);
                    break;
                case "get_merkle_path":
                    RpcGetMerklePath(ci);
                    break;
                case "SetBlockVolume":
                    RpcSetBlockVolume(ci);
                    break;
                default:
                    _logger.WriteError("Invalide command.");
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

        public CommandInfo ListAccounts(CommandInfo ci)
        {
            ci = _accountManager.ListAccount();
            return ci;
        }

        public CommandInfo UnlockAccount(CommandInfo ci)
        {
            ci = _accountManager.UnlockAccount(ci.Parameter.Split(" ")?[0], ci.Parameter.Split(" ")?[1], ci.Parameter.Split(" ")?[2]);
            return ci;
        }
        #endregion

        #region Rpc request methods

        public void RpcConnectChain(CommandInfo ci)
        {
            var req = RpcRequestManager.CreateRequest(new JObject(), "ConnectChain", 1);
            var resp = _requestManager.PostRequest(req.ToString(), out var returnCode, out var timeSpan);
            ci.TimeSpan = timeSpan;
            if (!CheckResponse(ci, returnCode, resp))
                return;
            
            var jObj = JObject.Parse(resp);

            var j = jObj["result"];
            if (j["error"] != null)
            {
               ci.ErrorMsg.Add(j["error"].ToString());
                return;
            }

            if (j["AElf.Contracts.Genesis"] != null)
            {
                _genesisAddress = j["AElf.Contracts.Genesis"].ToString();
            }

            if (j["ChainId"] != null)
            {
                _chainId = j["ChainId"].ToString();
                _accountManager = new AccountManager(_keyStore, _chainId);
                _transactionManager = new TransactionManager(_keyStore, _chainId);
            }

            var message = j.ToString();
            ci.InfoMsg.Add(message);
            ci.Result = true;
        }

        public void RpcLoadContractAbi(CommandInfo ci)
        {
            if (string.IsNullOrEmpty(ci.Parameter))
            {
                if (_genesisAddress == null)
                {
                    ci.ErrorMsg.Add("Please ConnectChain first.");
                    return;
                }
                ci.Parameter = _genesisAddress;
            }
            
            var req = RpcRequestManager.CreateRequest(new JObject
            {
                ["address"] = ci.Parameter
            }, "GetContractAbi", 1);
            if (!_loadedModules.TryGetValue(ci.Parameter, out var m))
            {
                string resp = _requestManager.PostRequest(req.ToString(), out var returnCode, out var timeSpan);
                ci.TimeSpan = timeSpan;
                if (!CheckResponse(ci, returnCode, resp))
                    return;
                JObject jObj = JObject.Parse(resp);
                var res = JObject.FromObject(jObj["result"]);
                        
                JToken ss = res["Abi"];
                byte[] aa = ByteArrayHelpers.FromHexString(ss.ToString());
                        
                MemoryStream ms = new MemoryStream(aa);
                m = Serializer.Deserialize<Module>(ms);
                _loadedModules.Add(ci.Parameter, m);
            }
                        
            var obj = JObject.FromObject(m);
            ci.InfoMsg.Add(obj.ToString());
            ci.Result = true;
        }

        public void RpcDeployContract(CommandInfo ci)
        {
            if (!ci.CheckParameterValid(3))
                return;
            string filename = ci.Parameter.Split(" ")[0];
            // Read sc bytes
            var screader = new SmartContractReader();
            byte[] sc = screader.Read(filename);
            string hex = sc.ToHex();

            if (!_loadedModules.TryGetValue(_genesisAddress, out var m))
            {
                ci.ErrorMsg.Add("ABI not loaded.");
                return;
            }

            var meth = m.Methods.FirstOrDefault(mt => mt.Name.Equals("DeploySmartContract"));
            if (meth == null)
            {
                ci.ErrorMsg.Add("Method not Found.");
                return;
            }
            byte[] serializedParams = meth.SerializeParams(new List<string> {"1", hex});
            _transactionManager.SetCmdInfo(ci);
            var tx = _transactionManager.CreateTransaction(ci.Parameter.Split(" ")[2], _genesisAddress,
                ci.Parameter.Split(" ")[1],
                "DeploySmartContract", serializedParams, TransactionType.ContractTransaction);
            tx = tx.AddBlockReference(_rpcAddress);
            if (tx == null)
                return;
            tx = _transactionManager.SignTransaction(tx);
            if (tx == null)
                return;
            var rawtx = _transactionManager.ConvertTransactionRawTx(tx);
            var req = RpcRequestManager.CreateRequest(rawtx, "BroadcastTransaction", 1);
            string resp = _requestManager.PostRequest(req.ToString(), out var returnCode, out var timeSpan);
            ci.TimeSpan = timeSpan;
            if (!CheckResponse(ci, returnCode, resp))
            {
                ci.Result = false;
                ci.ErrorMsg.Add(returnCode);
                return;
            }

            var jObj = JObject.Parse(resp);
            var j = jObj["result"];
            if (j["error"] != null)
            {
                ci.ErrorMsg.Add(j["error"].ToString());
                ci.Result = false;
                return;
            }
            string hash = j["hash"] == null ? j["error"]?.ToString() :j["hash"].ToString();
            string res = j["hash"] == null ? "error" : "txId";
            var jobj = new JObject
            {
                [res] = hash
            };
            ci.InfoMsg.Add(jobj.ToString());
            
            ci.Result = true;
        }
        
        public void RpcBroadcastTx(CommandInfo ci)
        {
            if (!ci.Parameter.Contains("{"))
            {
                RpcBroadcastWithRawTx(ci);
                return;
            }
            var j = JObject.Parse(ci.Parameter);
            var tr = _transactionManager.ConvertFromJson(j);
            if (tr == null)
                return;
            string toAdr = tr.To.GetFormatted();

            if (!_loadedModules.TryGetValue(toAdr, out var m))
            {
                if (!_loadedModules.TryGetValue(toAdr, out m))
                {
                    ci.ErrorMsg.Add("Abi Not Loaded.");
                    return;
                }
            }

            var method = m.Methods?.FirstOrDefault(mt => mt.Name.Equals(tr.MethodName));

            if (method == null)
            {
                ci.ErrorMsg.Add("Method not found.");
                return;
            }
                            
            var p = j["params"] == null ? null : JArray.Parse(j["params"].ToString());
            if (p != null)
            {
                var paramArray = p.ToObject<string[]>();

                if(j["params"] != null && paramArray.Length != 0)
                    tr.Params = method.SerializeParams(paramArray);
            }

            tr.Type = TransactionType.ContractTransaction;
            tr = tr.AddBlockReference(_rpcAddress);
            
            _transactionManager.SignTransaction(tr);
            var rawtx = _transactionManager.ConvertTransactionRawTx(tr);
            var req = RpcRequestManager.CreateRequest(rawtx, ci.Category, 1);
            
            
            string resp = _requestManager.PostRequest(req.ToString(), out var returnCode, out var timeSpan);
            ci.TimeSpan = timeSpan;
            if (!CheckResponse(ci, returnCode, resp))
                return;
            
            JObject rObj = JObject.Parse(resp);
            var rj = rObj["result"];
            string hash = rj["hash"] == null ? rj["error"].ToString() :rj["hash"].ToString();
            string res =rj["hash"] == null ? "error" : "txId";
            var jobj = new JObject
            {
                [res] = hash
            };
            ci.InfoMsg.Add(jobj.ToString());
            
            ci.Result = true;
        }

        public void RpcBroadcastWithRawTx(CommandInfo ci)
        {
            var rawtx = new JObject
            {
                ["rawtx"] = ci.Parameter
            };
            var req = RpcRequestManager.CreateRequest(rawtx, "BroadcastTransaction", 1);
            
            
            string resp = _requestManager.PostRequest(req.ToString(), out var returnCode, out var timeSpan);
            ci.TimeSpan = timeSpan;
            if (!CheckResponse(ci, returnCode, resp))
                return;
            
            var rObj = JObject.Parse(resp);
            var rj = rObj["result"];
            string hash = rj["hash"] == null ? rj["error"].ToString() :rj["hash"].ToString();
            string res =rj["hash"] == null ? "error" : "txId";
            var jobj = new JObject
            {
                [res] = hash
            };
            ci.InfoMsg.Add(jobj.ToString());
            
            ci.Result = true;
        }
        
        public string RpcGenerateTransactionRawTx(CommandInfo ci)
        {
            var j = JObject.Parse(ci.Parameter);
            var tr = _transactionManager.ConvertFromJson(j);
            var toAdr = tr.To.GetFormatted();

            if (!_loadedModules.TryGetValue(toAdr, out var m))
            {
                if (!_loadedModules.TryGetValue(toAdr, out m))
                {
                    ci.ErrorMsg.Add("Abi Not Loaded.");
                    return string.Empty;
                }
            }

            var method = m.Methods?.FirstOrDefault(mt => mt.Name.Equals(tr.MethodName));

            if (method == null)
            {
                ci.ErrorMsg.Add("Method not found.");
                return string.Empty;
            }
                            
            var p = j["params"] == null ? null : JArray.Parse(j["params"].ToString());
            if (p != null)
            {
                var paramArray = p.ToObject<string[]>();
                if(j["params"] != null && paramArray.Length != 0)
                    tr.Params = method.SerializeParams(paramArray);
            }

            tr.Type = TransactionType.ContractTransaction;
            tr = tr.AddBlockReference(_rpcAddress);
            
            _transactionManager.SignTransaction(tr);
            var rawtx = _transactionManager.ConvertTransactionRawTx(tr);
            
            return rawtx["rawtx"].ToString();
        }

        public string RpcGenerateTransactionRawTx(string from, string to, string methodName, params string[] paramArray)
        {
            var tr = new Transaction()
            {
                From = Address.Parse(from),
                To = Address.Parse(to),
                MethodName = methodName
            };
            string toAdr = tr.To.GetFormatted();

            Module m;
            if (!_loadedModules.TryGetValue(toAdr, out m))
            {
                if (!_loadedModules.TryGetValue(toAdr, out m))
                {
                    _logger.WriteError("Abi Not Loaded.");
                    return string.Empty;
                }
            }

            var method = m.Methods?.FirstOrDefault(mt => mt.Name.Equals(tr.MethodName));

            if (method == null)
            {
                _logger.WriteError("Method not found.");
                return string.Empty;
            }

            if (paramArray != null && paramArray.Length != 0)
                tr.Params = method.SerializeParams(paramArray);
            tr.Type = TransactionType.ContractTransaction;
            tr = tr.AddBlockReference(_rpcAddress);

            _transactionManager.SignTransaction(tr);
            var rawtx = _transactionManager.ConvertTransactionRawTx(tr);

            return rawtx["rawtx"].ToString();
        }

        public void RpcBroadcastTxs(CommandInfo ci)
        {
            var paramObject = new JObject
            {
                ["rawtxs"] = ci.Parameter
            };
            var req = RpcRequestManager.CreateRequest(paramObject, ci.Category, 0);
            string resp = _requestManager.PostRequest(req.ToString(), out var returnCode, out var timeSpan);
            ci.TimeSpan = timeSpan;
            if (!CheckResponse(ci, returnCode, resp))
                return;
            
            JObject jObj = JObject.Parse(resp);
            var j = jObj["result"];
            ci.InfoMsg.Add(j["result"].ToString());
            ci.Result = true;
        }
        
        public void RpcGetCommands(CommandInfo ci)
        {
            var req = RpcRequestManager.CreateRequest(new JObject(), ci.Category, 0);
            string resp = _requestManager.PostRequest(req.ToString(), out var returnCode, out var timeSpan);
            ci.TimeSpan = timeSpan;
            if (!CheckResponse(ci, returnCode, resp))
                return;
            
            JObject jObj = JObject.Parse(resp);
            var j = jObj["result"];
            ci.InfoMsg.Add(j["result"]["commands"].ToString());
            ci.Result = true;
        }
        
        public void RpcGetContractAbi(CommandInfo ci)
        {
            if (ci.Parameter == "")
            {
                if (_genesisAddress == null)
                {
                    ci.ErrorMsg.Add("Please ConnectChain first.");
                    return;
                }
                ci.Parameter = _genesisAddress;
            }
            
            var req = RpcRequestManager.CreateRequest(new JObject
            {
                ["address"] = ci.Parameter
            }, ci.Category, 1);
            

            if (!_loadedModules.TryGetValue(ci.Parameter, out var m))
            {
                string resp = _requestManager.PostRequest(req.ToString(), out var returnCode, out var timeSpan);
                ci.TimeSpan = timeSpan;
                if (!CheckResponse(ci, returnCode, resp))
                    return;

                JObject jObj = JObject.Parse(resp);
                var res = JObject.FromObject(jObj["result"]);
                        
                JToken ss = res["abi"];
                byte[] aa = ByteArrayHelpers.FromHexString(ss.ToString());
                        
                MemoryStream ms = new MemoryStream(aa);
                m = Serializer.Deserialize<Module>(ms);
                _loadedModules.Add(ci.Parameter, m);
                ci.InfoMsg.Add(resp);
            }

            var obj = JObject.FromObject(m);
            ci.InfoMsg.Add(obj.ToString());
            ci.Result = true;
        }
        
        public void RpcGetIncrement(CommandInfo ci)
        {
            if (!ci.CheckParameterValid(1))
                return;
            
            var req = RpcRequestManager.CreateRequest(new JObject
            {
                ["address"] = ci.Parameter
            }, ci.Category, 1);
            var resp = _requestManager.PostRequest(req.ToString(), out var returnCode, out var timeSpan);
            ci.TimeSpan = timeSpan;
            if (!CheckResponse(ci, returnCode, resp))
                return;
            ci.InfoMsg.Add(resp);
            ci.Result = true;
        }
        
        public void RpcGetTxResult(CommandInfo ci)
        {
            if (!ci.CheckParameterValid(1))
                return;
            
            var req = RpcRequestManager.CreateRequest(new JObject
            {
                ["txhash"] = ci.Parameter
            }, ci.Category, 0);
            string resp = _requestManager.PostRequest(req.ToString(), out var returnCode, out var timeSpan);
            ci.TimeSpan = timeSpan;
            if (!CheckResponse(ci, returnCode, resp))
                return;
            ci.InfoMsg.Add(resp);
            ci.Result = true;
        }
        
        public void RpcGetBlockHeight(CommandInfo ci)
        {
            var req = RpcRequestManager.CreateRequest(new JObject(), ci.Category, 0);
            string resp = _requestManager.PostRequest(req.ToString(), out var returnCode, out var timeSpan);
            ci.TimeSpan = timeSpan;
            if (!CheckResponse(ci, returnCode, resp))
                return;
            ci.InfoMsg.Add(resp);
            ci.Result = true;
        }
        
        public void RpcGetBlockInfo(CommandInfo ci)
        {
            if (!ci.CheckParameterValid(2))
                return;
            
            var req = RpcRequestManager.CreateRequest(new JObject
            {
                ["block_height"] = ci.Parameter.Split(" ")?[0],
                ["include_txs"] = ci.Parameter.Split(" ")?[1]
            }, ci.Category, 0);
            string resp = _requestManager.PostRequest(req.ToString(), out var returnCode, out var timeSpan);
            ci.TimeSpan = timeSpan;
            if (!CheckResponse(ci, returnCode, resp))
                return;
            ci.InfoMsg.Add(resp);
            ci.Result = true;
        }

        public void RpcGetMerklePath(CommandInfo ci)
        {
            if (!ci.CheckParameterValid(1))
                return;

            var req = RpcRequestManager.CreateRequest(new JObject
            {
                ["txid"] = ci.Parameter

            }, ci.Category, 1);
            
            
            string resp = _requestManager.PostRequest(req.ToString(), out var returnCode, out var timeSpan);
            ci.TimeSpan = timeSpan;
            if (!CheckResponse(ci, returnCode, resp))
                return;
            ci.InfoMsg.Add(resp);
            ci.Result = true;
        }
        
        public void RpcSetBlockVolume(CommandInfo ci)
        {
            if (!ci.CheckParameterValid(2))
                return;
            
            var req = RpcRequestManager.CreateRequest(new JObject
            {
                ["minimal"] = ci.Parameter.Split(" ")?[0],
                ["maximal"] = ci.Parameter.Split(" ")?[1]
            }, ci.Category, 1);
            
            
            string resp = _requestManager.PostRequest(req.ToString(), out var returnCode, out var timeSpan);
            ci.TimeSpan = timeSpan;
            if (!CheckResponse(ci, returnCode, resp))
                return;
            ci.InfoMsg.Add(resp);
            ci.Result = true;
        }

        public string RpcQueryResult(string from, string to, string methodName, params string[] paramArray)
        {
            var tr = new Transaction()
            {
                From = Address.Parse(from),
                To = Address.Parse(to),
                MethodName = methodName
            };

            string toAdr = tr.To.GetFormatted();

            if (!_loadedModules.TryGetValue(toAdr, out var m))
            {
                if (!_loadedModules.TryGetValue(toAdr, out m))
                {
                    _logger.WriteError("Abi Not Loaded.");
                    return string.Empty;
                }
            }

            var method = m.Methods?.FirstOrDefault(mt => mt.Name.Equals(tr.MethodName));

            if (method == null)
            {
                _logger.WriteError("Method not found.");
                return string.Empty;
            }

            if (paramArray != null && paramArray.Length != 0)
                tr.Params = method.SerializeParams(paramArray);

            var resp = CallTransaction(tr, "call");

            return resp;
        }

        private string CallTransaction(Transaction tx, string api)
        {
            MemoryStream ms = new MemoryStream();
            Serializer.Serialize(ms, tx);

            byte[] b = ms.ToArray();
            string payload = b.ToHex();
            var reqParams = new JObject { ["rawtx"] = payload };
            var req = RpcRequestManager.CreateRequest(reqParams, api, 1);

            string resp = _requestManager.PostRequest(req.ToString(), out var returnCode, out var timeSpan);

            return resp;
        }

        public string GetPublicKeyFromAddress(string account, string password = "123")
        {
            _accountManager.UnlockAccount(account, password, "notimeout");
            return _accountManager.GetPublicKey(account);
        }

        //Net Api
        public void NetGetPeers(CommandInfo ci)
        {
            var req = RpcRequestManager.CreateRequest(new JObject(), "get_peers", 1);
            string resp = _requestManager.PostRequest(req.ToString(), out var returnCode, out var timeSpan);
            ci.TimeSpan = timeSpan;
            if (!CheckResponse(ci, returnCode, resp))
                return;

            ci.InfoMsg.Add(resp);
            ci.Result = true;
        }

        public void NetAddPeer(CommandInfo ci)
        {
            var req = RpcRequestManager.CreateRequest(new JObject
            {
                ["address"] = ci.Parameter
            }, "add_peer", 1);
            string resp = _requestManager.PostRequest(req.ToString(), out var returnCode, out var timeSpan);
            ci.TimeSpan = timeSpan;
            if (!CheckResponse(ci, returnCode, resp))
                return;

            ci.InfoMsg.Add(resp);
            ci.Result = true;
        }

        public void NetRemovePeer(CommandInfo ci)
        {
            var req = RpcRequestManager.CreateRequest(new JObject
            {
                ["address"] = ci.Parameter
            }, "remove_peer", 1);
            string resp = _requestManager.PostRequest(req.ToString(), out var returnCode, out var timeSpan);
            ci.TimeSpan = timeSpan;
            if (!CheckResponse(ci, returnCode, resp))
                return;

            ci.InfoMsg.Add(resp);
            ci.Result = true;
        }
        
        #endregion
        
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

            if (response.IsEmpty())
            {
                ci.ErrorMsg.Add("Failed. Pleas check input.");
                return false;
            }

            return true;
        }

        private ulong GetRandomIncrId()
        {
            return Convert.ToUInt64(DateTime.Now.ToString("MMddHHmmss") + DateTime.Now.Millisecond.ToString());
        }

    }
}