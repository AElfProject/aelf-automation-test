using System;
using System.Collections.Generic;
using AElf.Automation.Common.Extensions;
using AElf.Automation.Common.Helpers;
using Newtonsoft.Json.Linq;
using NServiceKit.Common.Extensions;

namespace AElf.Automation.RpcTesting
{
    public class RpcAPI
    {
        public string RpcUrl { get; set; }
        public CliHelper CH { get; set; }
        public ILogHelper Logger = LogHelper.GetLogHelper();

        public RpcAPI(string rpcUrl)
        {
            RpcUrl = rpcUrl;
            CH = new CliHelper(rpcUrl);
        }

        public int GetCurrentHeight()
        {
            var ci = new CommandInfo("get_block_height");
            CH.ExecuteCommand(ci);
            if (ci.Result)
            {
                ci.GetJsonInfo();
                return Int32.Parse(ci.JsonInfo["result"]["result"]["block_height"].ToString());
            }
            else
            {
                Logger.WriteError(ci.ErrorMsg?[0]);
                return 0;
            }
        }

        public JObject GetBlockInfo(int height)
        {
            var ci = new CommandInfo("get_block_info");
            ci.Parameter = $"{height.ToString()} true";
            CH.ExecuteCommand(ci);
            if (ci.Result)
            {
                ci.GetJsonInfo();
                return ci.JsonInfo;
            }
            else
            {
                Logger.WriteError(ci.ErrorMsg?[0]);
                return null;
            }
        }

        public JObject GetTxResult(string txHash)
        {
            var ci = new CommandInfo("get_tx_result");
            ci.Parameter = txHash;
            CH.ExecuteCommand(ci);
            if (ci.Result)
            {
                ci.GetJsonInfo();
                return ci.JsonInfo;
            }
            else
            {
                Logger.WriteError(ci.ErrorMsg?[0]);
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