using AElf.Automation.Common.Helpers;
using System.Collections.Generic;
using log4net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AElf.Automation.Common.OptionManagers
{
    public class RpcRequestManager
    {
        private string RpcMethod { get; set; }
        private string RpcParameter { get; set; }
        private string RpcBody { get; set; }
        private string RpcUrl { get; set; }
        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        public RpcRequestManager(string url, string path = "chain")
        {
            if (url.Contains("/chain") || url.Contains("/net") || url.Contains("/wallet"))
                RpcUrl = url;
            else
                RpcUrl = $"{url}/{path}";
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

        public JObject PostRequest(string method, JObject requestData, int id = 0)
        {
            var requestString = CreateRequest(requestData, method, id).ToString();
            string response = PostRequest(method, requestString, out var returnCode);
            var result = CheckRpcRequestResult(returnCode, response);
            if (result)
                return JsonConvert.DeserializeObject<JObject>(response);

            return new JObject();
        }

        public JObject PostRequest(string method)
        {
            var requestString = CreateRequest(new JObject(), method, 0).ToString();
            var response = PostRequest(method, requestString, out var returnCode);
            var result = CheckRpcRequestResult(returnCode, response);
            return result ? JsonConvert.DeserializeObject<JObject>(response) : new JObject();
        }

        public JObject PostRequest(ApiMethods api)
        {
            var method = api.ToString();
            var requestString = CreateRequest(new JObject(), method, 0).ToString();
            var response = PostRequest(method, requestString, out var returnCode);
            var result = CheckRpcRequestResult(returnCode, response);
            return result ? JsonConvert.DeserializeObject<JObject>(response) : new JObject();
        }

        public string PostRequest(List<string> rpcBody, out string returnCode)
        {
            RpcMethod = "SendTransactions";
            foreach (var rpc in rpcBody)
            {
                RpcParameter += "," + rpc;
            }

            RpcParameter = RpcParameter.Substring(1);
            RpcBody = "{\"jsonrpc\":\"2.0\",\"method\":\"" + RpcMethod + "\",\"params\":{\"rawtxs\":\"" + RpcParameter +
                      "\"},\"id\":0}";

            return HttpHelper.PostResponse(RpcUrl, RpcBody, out returnCode);
        }

        public static JObject CreateRequest(JObject requestData, string method, int id)
        {
            var jObj = new JObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id,
                ["method"] = method,
                ["params"] = requestData
            };

            return jObj;
        }

        private bool CheckRpcRequestResult(string returnCode, string response)
        {
            if (response == null)
            {
                Logger.Error("Could not connect to server.");
                return false;
            }

            if (returnCode != "OK")
            {
                Logger.Error("Http request failed, status: " + returnCode);
                return false;
            }

            if (!string.IsNullOrEmpty(response)) return true;

            Logger.Error("Failed. Pleas check input.");
            return false;
        }
    }
}