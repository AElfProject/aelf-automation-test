using AElf.Automation.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace AElf.Automation.Common.Extensions
{
    public class RpcRequest
    {
        public string RpcMethod { get; set; }
        public string RpcParameter { get; set; }
        public string RpcBody { get; set; }
        public string RpcUrl { get; set; }
        public RpcRequest(string url)
        {
            RpcUrl = url;
            //RpcBody = string.Format("{\"jsonrpc\":\"2.0\",\"method\":\"{0}\",\"params\":{{1}},\"id\":0}", RpcMethod, RpcParameter);
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
            Console.WriteLine("One thread rpc request generated successfully.");

            return HttpHelper.PostResponse(RpcUrl, RpcBody, out returnCode);
        }
    }
}
