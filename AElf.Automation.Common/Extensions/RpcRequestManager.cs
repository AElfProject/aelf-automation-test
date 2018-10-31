using AElf.Automation.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

namespace AElf.Automation.Common.Extensions
{
    public class RpcRequestManager
    {
        public string RpcMethod { get; set; }
        public string RpcParameter { get; set; }
        public string RpcBody { get; set; }
        public string RpcUrl { get; set; }
        
        public RpcRequestManager(string url)
        {
            RpcUrl = url;
        }

        public string PostRequest(string body, out string returnCode, out long timeSpan)
        {
            timeSpan = 0;
            return HttpHelper.PostResponse(RpcUrl, body, out returnCode, out timeSpan);
        }

        public string PostRequest(string method, string parameter, out string returnCode)
        {
            RpcMethod = method;
            RpcParameter = parameter;
            RpcBody = "{\"jsonrpc\":\"2.0\",\"method\":\"" + RpcMethod + "\",\"params\":" + RpcParameter + ",\"id\":0}";
            return HttpHelper.PostResponse(RpcUrl, RpcBody, out returnCode);
        }

        public string PostRequest(string method, List<string> rpcBody, out string returnCode)
        {
            RpcMethod = method;
            foreach(var rpc in rpcBody)
            {
                RpcParameter += "," + rpc;
            }
            RpcParameter = RpcParameter.Substring(1);
            RpcBody = "{\"jsonrpc\":\"2.0\",\"method\":\"" + RpcMethod + "\",\"params\":{\"rawtxs\":\"" + RpcParameter + "\"},\"id\":0}";

            return HttpHelper.PostResponse(RpcUrl, RpcBody, out returnCode);
        }
        
        public static JObject CreateRequest(JObject requestData, string method, int id)
        {
            JObject jObj = new JObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id,
                ["method"] = method,
                ["params"] = requestData
            };

            return jObj;
        }
    }
}
