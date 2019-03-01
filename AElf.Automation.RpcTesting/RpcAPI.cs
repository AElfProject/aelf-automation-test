using System;
using System.Collections.Generic;
using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;
using Newtonsoft.Json.Linq;
using NServiceKit.Common.Extensions;

namespace AElf.Automation.RpcTesting
{
    public class RpcApi
    {
        private string RpcUrl { get; set; }
        private CliHelper Ch { get; set; }
        private readonly ILogHelper _logger = LogHelper.GetLogHelper();

        public RpcApi(string rpcUrl)
        {
            RpcUrl = rpcUrl;
            Ch = new CliHelper(rpcUrl);
        }

        public int GetCurrentHeight()
        {
            var ci = new CommandInfo("GetBlockHeight");
            Ch.ExecuteCommand(ci);
            if (ci.Result)
            {
                ci.GetJsonInfo();
                return Int32.Parse(ci.JsonInfo["result"]["result"]["block_height"].ToString());
            }
            else
            {
                _logger.WriteError(ci.ErrorMsg?[0]);
                return 0;
            }
        }

        public JObject GetBlockInfo(int height)
        {
            var ci = new CommandInfo("GetBlockInfo");
            ci.Parameter = $"{height.ToString()} true";
            Ch.ExecuteCommand(ci);
            if (ci.Result)
            {
                ci.GetJsonInfo();
                return ci.JsonInfo;
            }
            else
            {
                _logger.WriteError(ci.ErrorMsg?[0]);
                return null;
            }
        }

        public JObject GetTxResult(string txHash)
        {
            var ci = new CommandInfo("GetTransactionResult");
            ci.Parameter = txHash;
            Ch.ExecuteCommand(ci);
            if (ci.Result)
            {
                ci.GetJsonInfo();
                return ci.JsonInfo;
            }
            else
            {
                _logger.WriteError(ci.ErrorMsg?[0]);
                return null;
            }
        }
    }

    public class BlockInfo
    {
        public int Height { get; set; }
        public string BlockHash { get; set; }
        public string PreviousBlockHash { get; set; }
        public string MerkleTreeRootOfTransactions { get; set; }
        public string MerkleTreeRootOfWorldState { get; set; }
        public List<string> Transactions { get; set; }

        public BlockInfo(int height, JObject jsonInfo)
        {
            Height = height;
            var resultInfo = jsonInfo["result"]["result"];
            BlockHash = resultInfo["Blockhash"].ToString();
            PreviousBlockHash = resultInfo["Header"]["PreviousBlockHash"].ToString();
            MerkleTreeRootOfTransactions = resultInfo["Header"]["MerkleTreeRootOfTransactions"].ToString();
            MerkleTreeRootOfWorldState = resultInfo["Header"]["MerkleTreeRootOfWorldState"].ToString();
            Transactions = resultInfo["Body"]["Transactions"].ToString().Replace("[", "").Replace("]", "").Replace("\n", "").Replace("\"", "").Trim().Split(",").ToList<string>();
        }
    }
}