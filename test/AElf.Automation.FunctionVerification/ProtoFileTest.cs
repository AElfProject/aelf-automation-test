using System;
using AElf.Contracts.MultiToken;
using AElfChain.Common.Contracts;
using AElfChain.Common.Contracts.Serializer;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using log4net;
using Newtonsoft.Json.Linq;

namespace AElf.Automation.ContractsTesting
{
    public class ProtoFileTest
    {
        private readonly INodeManager _nodeManager;

        public ILog Logger = Log4NetHelper.GetLogger();

        public ProtoFileTest(INodeManager nodeManager)
        {
            _nodeManager = nodeManager;

            //AnalyzeTokenContractInfo();
            AnalyzeTokenContract();
        }

        public void AnalyzeTokenContractInfo()
        {
            var contractDescriptor = TokenContractContainer.Descriptor;
            foreach (var method in contractDescriptor.Methods)
            {
                var name = method.Name;
                var input = method.InputType.Name;
                var output = method.OutputType.Name;
                Logger.Info($"Method: {name}, Input: {input}, Output: {output}");

                var parameters = method.InputType.Fields.InFieldNumberOrder();
                foreach (var parameter in parameters)
                {
                    var jsonName = parameter.JsonName;
                    var index = parameter.Index;
                    var fieldType = parameter.FieldType;
                    Logger.Info($"Index: {index}, Name: {jsonName}, FieldType: {fieldType}");
                }

                Console.WriteLine();
            }
        }

        public void AnalyzeTokenContract()
        {
            var contractHandler = new ContractSerializer();
            var contractInfo = contractHandler.GetContractInfo(NameProvider.Token);
            var methodInfo = contractInfo.GetContractMethod("GetBalance");

            var parameterInput = new[] {"TELF", "28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK"};
            var jsonObject = new JObject();
            for (var i = 0; i < methodInfo.InputFields.Count; i++)
            {
                var type = methodInfo.InputFields[i];
                if (type.FieldType == FieldType.Message && type.MessageType.Name == "Address")
                {
                    if (type.MessageType.Name == "Address")
                        jsonObject[methodInfo.InputFields[i].Name] = new JObject
                        {
                            ["value"] = parameterInput[i].ConvertAddress().Value.ToBase64()
                        };
                    else if (type.MessageType.Name == "Hash")
                        jsonObject[methodInfo.InputFields[i].Name] = new JObject
                        {
                            ["value"] = HashHelper.HexStringToHash(parameterInput[i]).Value.ToBase64()
                        };
                }
                else
                {
                    jsonObject[methodInfo.InputFields[i].Name] = parameterInput[i];
                }
            }

            var jsonInfo = jsonObject.ToString();
            jsonInfo.WriteSuccessLine();

            var input = JsonParser.Default.Parse(jsonInfo, methodInfo.InputType);

            var byteString = _nodeManager.QueryView("28Y8JA1i2cN6oHvdv7EraXJr9a1gY6D1PpJXw9QtRMRwKcBQMK",
                "mS8xMLs9SuWdNECkrfQPF8SuRXRuQzitpjzghi3en39C3SRvf",
                "GetBalance", input);
            var instance = (IMessage) Activator.CreateInstance(methodInfo.OutputType.ClrType);
            instance.MergeFrom(byteString.ToByteArray());
            Logger.Info(JsonFormatter.ToDiagnosticString(instance));
        }
    }
}