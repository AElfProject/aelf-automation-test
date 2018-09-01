using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.Automation.Common.Extensions;
using AElf.Cryptography;
using AElf.Common.Application;
using AElf.Common.Extensions;
using AElf.Common.ByteArrayHelpers;
using Newtonsoft.Json.Linq;
using NServiceKit.Common;
using ProtoBuf;
using Globals = AElf.Kernel.Globals;
using Method = AElf.Automation.Common.Protobuf.Method;
using Module = AElf.Automation.Common.Protobuf.Module;
using Transaction = AElf.Automation.Common.Protobuf.Transaction;
using TransactionType = AElf.Automation.Common.Protobuf.TransactionType;

namespace AElf.Automation.Common.Helpers
{
    public class CliHelper
    {
        private string _rpcAddress;
        private string _genesisAddress;
        private AElfKeyStore _keyStore;
        private AccountManager _accountManager;
        private TransactionManager _transactionManager;
        private RpcRequestManager _requestManager;
        
        private Dictionary<string, Module> _loadedModules;
        private ILogHelper Logger = LogHelper.GetLogHelper();

        
        public List<CommandInfo> CommandList { get; set; }

        public CliHelper(string rpcUrl)
        {
            _keyStore = new AElfKeyStore(ApplicationHelpers.GetDefaultDataDir());
            _accountManager = new AccountManager(_keyStore);
            _transactionManager = new TransactionManager(_keyStore);
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
                    ci = _accountManager.NewAccount(ci.Parameter);
                    break;
                case "account list":
                    ci = _accountManager.ListAccount();
                    break;
                case "account unlock":
                    ci = _accountManager.UnlockAccount(ci.Parameter.Split(" ")?[0], ci.Parameter.Split(" ")?[1], ci.Parameter.Split(" ")?[2]);
                    break;
                case "connect_chain":
                    RpcConnectChain(ci);
                    break;
                case "load_contract_abi":
                    RpcLoadContractAbi(ci);
                    break;
                case "deploy_contract":
                    RpcDeployContract(ci);
                    break;
                case "broadcast_tx":
                    RpcBroadcastTx(ci);
                    break;
                case "broadcast_txs":
                    RpcBroadcastTxs(ci);
                    break;
                case "get_commands":
                    RpcGetCommands(ci);
                    break;
                case "get_contract_abi":
                    RpcGetContractAbi(ci);
                    break;
                case "get_increment":
                    RpcGetIncrement(ci);
                    break;
                case "get_tx_result":
                    RpcGetTxResult(ci);
                    break;
                case "get_block_height":
                    RpcGetBlockHeight(ci);
                    break;
                case "get_block_info":
                    RpcGetBlockInfo(ci);
                    break;
                case "set_block_volume":
                    RpcSetBlockVolume(ci);
                    break;
                default:
                    Logger.WriteInfo("Invalide command.");
                    break;
            }
            
            ci.PrintResultMessage();
            CommandList.Add(ci);

            return ci;
        }
        
        #region Rpc request methods
        
        public void RpcConnectChain(CommandInfo ci)
        {
            var req = RpcRequestManager.CreateRequest(new JObject(), "connect_chain", 1);
            string returnCode = string.Empty;
            long timeSpan = 0;
            string resp = _requestManager.PostRequest(req.ToString(), out returnCode, out timeSpan);
            ci.TimeSpan = timeSpan;
            if (!CheckResponse(ci, returnCode, resp))
                return;
            
            JObject jObj = JObject.Parse(resp);

            var j = jObj["result"];
            if (j["error"] != null)
            {
               ci.ErrorMsg.Add(j["error"].ToString());
                return;
            }

            if (j["result"]["BasicContractZero"] != null)
            {
                _genesisAddress = j["result"]["BasicContractZero"].ToString();
            }
            string message = JObject.FromObject(j["result"]).ToString();
            ci.InfoMsg.Add(message);
            ci.Result = true;
        }

        public void RpcLoadContractAbi(CommandInfo ci)
        {
            if (ci.Parameter == "")
            {
                if (_genesisAddress == null)
                {
                    ci.ErrorMsg.Add("Please connect_chain first.");
                    return;
                }
                ci.Parameter = _genesisAddress;
            }
            
            var req = RpcRequestManager.CreateRequest(new JObject
            {
                ["address"] = ci.Parameter
            }, "get_contract_abi", 1);
            Module m = null;
            if (!_loadedModules.TryGetValue(ci.Parameter, out m))
            {
                            
                string returnCode = string.Empty;
                long timeSpan = 0;
                string resp = _requestManager.PostRequest(req.ToString(), out returnCode, out timeSpan);
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
            SmartContractReader screader = new SmartContractReader();
            byte[] sc = screader.Read(filename);
            string hex = sc.ToHex();

            var name = Globals.GenesisBasicContract;
            Module m = _loadedModules.Values.FirstOrDefault(ld => ld.Name.Equals(name));
            if (m == null)
            {
                ci.ErrorMsg.Add("ABI not loaded.");
                return;
            }
            Method meth = m.Methods.FirstOrDefault(mt => mt.Name.Equals("DeploySmartContract"));
            if (meth == null)
            {
                ci.ErrorMsg.Add("Method not Found.");
                return;
            }
            byte[] serializedParams = meth.SerializeParams(new List<string> {"1", hex} );
            _transactionManager.SetCmdInfo(ci);
            Transaction tx = _transactionManager.CreateTransaction(ci.Parameter.Split(" ")[2], _genesisAddress,
                ci.Parameter.Split(" ")[1],
                "DeploySmartContract", serializedParams, TransactionType.ContractTransaction);
            if (tx == null)
                return;
            tx = _transactionManager.SignTransaction(tx);
            if (tx == null)
                return;
            var rawtx = _transactionManager.ConvertTransactionRawTx(tx);
            var req = RpcRequestManager.CreateRequest(rawtx, "broadcast_tx", 1);
            string returnCode = string.Empty;
            long timeSpan = 0;
            string resp = _requestManager.PostRequest(req.ToString(), out returnCode, out timeSpan);
            ci.TimeSpan = timeSpan;
            if (!CheckResponse(ci, returnCode, resp))
                return;
            
            JObject jObj = JObject.Parse(resp);
            var j = jObj["result"];
            string hash = j["hash"] == null ? j["error"].ToString() :j["hash"].ToString();
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
            JObject j = JObject.Parse(ci.Parameter);
            Transaction tr = _transactionManager.ConvertFromJson(j);
            if (tr == null)
                return;
            string hex = tr.To.Value.ToHex();
            Module m = null;
            if (!_loadedModules.TryGetValue(hex.Replace("0x", ""), out m))
            {
                if (!_loadedModules.TryGetValue("0x"+hex.Replace("0x", ""), out m))
                {
                    ci.ErrorMsg.Add("Abi Not Loaded.");
                    return;
                }
            }

            Method method = m.Methods?.FirstOrDefault(mt => mt.Name.Equals(tr.MethodName));

            if (method == null)
            {
                ci.ErrorMsg.Add("Method not found.");
                return;
            }
                            
            JArray p = j["params"] == null ? null : JArray.Parse(j["params"].ToString());
            tr.Params = j["params"] == null ? null : method.SerializeParams(p.ToObject<string[]>());
            tr.type = TransactionType.ContractTransaction;
                            
            _transactionManager.SignTransaction(tr);
            var rawtx = _transactionManager.ConvertTransactionRawTx(tr);
            var req = RpcRequestManager.CreateRequest(rawtx, ci.Category, 1);
            string returnCode = string.Empty;
            long timeSpan = 0;
            string resp = _requestManager.PostRequest(req.ToString(), out returnCode, out timeSpan);
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
            var req = RpcRequestManager.CreateRequest(rawtx, "broadcast_tx", 1);
            string returnCode = string.Empty;
            long timeSpan = 0;
            string resp = _requestManager.PostRequest(req.ToString(), out returnCode, out timeSpan);
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
        
        public string RpcGenerateTransactionRawTx(CommandInfo ci)
        {
            JObject j = JObject.Parse(ci.Parameter);
            Transaction tr = _transactionManager.ConvertFromJson(j);
            string hex = tr.To.Value.ToHex();
            Module m = null;
            if (!_loadedModules.TryGetValue(hex.Replace("0x", ""), out m))
            {
                if (!_loadedModules.TryGetValue("0x"+hex.Replace("0x", ""), out m))
                {
                    ci.ErrorMsg.Add("Abi Not Loaded.");
                    return string.Empty;
                }
            }

            Method method = m.Methods?.FirstOrDefault(mt => mt.Name.Equals(tr.MethodName));

            if (method == null)
            {
                ci.ErrorMsg.Add("Method not found.");
                return string.Empty;
            }
                            
            JArray p = j["params"] == null ? null : JArray.Parse(j["params"].ToString());
            tr.Params = j["params"] == null ? null : method.SerializeParams(p.ToObject<string[]>());
            tr.type = TransactionType.ContractTransaction;
                            
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
            string returnCode = string.Empty;
            long timeSpan = 0;
            string resp = _requestManager.PostRequest(req.ToString(), out returnCode, out timeSpan);
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
            string returnCode = string.Empty;
            long timeSpan = 0;
            string resp = _requestManager.PostRequest(req.ToString(), out returnCode, out timeSpan);
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
                    ci.ErrorMsg.Add("Please connect_chain first.");
                    return;
                }
                ci.Parameter = _genesisAddress;
            }
            
            var req = RpcRequestManager.CreateRequest(new JObject
            {
                ["address"] = ci.Parameter
            }, ci.Category, 1);
            
            Module m = null;
            if (!_loadedModules.TryGetValue(ci.Parameter, out m))
            {
                string returnCode = string.Empty;
                long timeSpan = 0;
                string resp = _requestManager.PostRequest(req.ToString(), out returnCode, out timeSpan);
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
            string returnCode = string.Empty;
            long timeSpan = 0;
            string resp = _requestManager.PostRequest(req.ToString(), out returnCode, out timeSpan);
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
            string returnCode = string.Empty;
            long timeSpan = 0;
            string resp = _requestManager.PostRequest(req.ToString(), out returnCode, out timeSpan);
            ci.TimeSpan = timeSpan;
            if (!CheckResponse(ci, returnCode, resp))
                return;
            ci.InfoMsg.Add(resp);
            ci.Result = true;
        }
        
        public void RpcGetBlockHeight(CommandInfo ci)
        {
            var req = RpcRequestManager.CreateRequest(new JObject(), ci.Category, 0);
            string returnCode = string.Empty;
            long timeSpan = 0;
            string resp = _requestManager.PostRequest(req.ToString(), out returnCode, out timeSpan);
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
            string returnCode = string.Empty;
            long timeSpan = 0;
            string resp = _requestManager.PostRequest(req.ToString(), out returnCode, out timeSpan);
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
            string returnCode = string.Empty;
            long timeSpan = 0;
            string resp = _requestManager.PostRequest(req.ToString(), out returnCode, out timeSpan);
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
    }
}