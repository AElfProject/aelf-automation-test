using System;
using AElf.Automation.Common.Extensions;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json.Linq;

namespace AElf.Automation.Common.Helpers
{
    public class RpcHelper
    {
        private readonly string _serviceUrl;
        private readonly ILogHelper _log = LogHelper.GetLogHelper();
        private RpcRequestManager _request;

        public RpcHelper(string serviceUrl)
        {
            _request = new RpcRequestManager(serviceUrl);
        }

        public JObject QueryCommands()
        {
            var api = "get_commands";
            return _request.PostRequest(api);
        }

        public JObject ConnectChain()
        {
            var api = "connect_chain";
            return _request.PostRequest(api);
        }

        public JObject QueryContractAbi(string address)
        {
            var api = "get_contract_abi";
            var requestData = new JObject
            {
                ["address"] = address
            };
            return _request.PostRequest(api, requestData, 1);
        }



        private bool CheckRpcRequestResult(string returnCode, string response)
        {
            if (response == null)
            {
                _log.WriteError("Could not connect to server.");
                return false;
            }

            if (returnCode != "OK")
            {
                _log.WriteError("Http request failed, status: " + returnCode);
                return false;
            }

            if (response == String.Empty)
            {
                _log.WriteError("Failed. Pleas check input.");
                return false;
            }

            return true;
        }
    }
}