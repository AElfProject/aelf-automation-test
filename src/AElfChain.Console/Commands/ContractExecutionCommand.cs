using System;
using System.Collections.Generic;
using System.Linq;
using AElf;
using AElf.Automation.Common.Contracts;
using AElf.Automation.Common.ContractSerializer;
using AElf.Automation.Common.Helpers;
using AElf.Automation.Common.Managers;
using AElf.Automation.Common.Utils;
using AElfChain.Console.InputOption;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Volo.Abp.Threading;

namespace AElfChain.Console.Commands
{
    public class ContractExecutionCommand : BaseCommand
    {
        private ContractHandler ContractHandler { get; set; }

        public ContractExecutionCommand(INodeManager nodeManager, ContractServices contractServices)
            : base(nodeManager, contractServices)
        {
            Logger = Log4NetHelper.GetLogger();
            ContractHandler = new ContractHandler();
        }

        public override void RunCommand()
        {
            "Select system contract name: ".WriteSuccessLine();
            var contractEngine = new CommandsCompletionEngine(ContractHandler.SystemContractsDescriptors.Keys
                .Select(o => o.ToString()).ToList());
            var contractReader = new ConsoleReader(contractEngine);
            var input = CommandOption.InputParameters(1, contractReader);
            if (input == null)
                return;
            var nameProvider = input[0].ConvertNameProvider();
            var contractInfo = ContractHandler.GetContractInfo(nameProvider);
            //contract info
            var contractAddress = Services.GetContractAddress(input[0]);
            if (contractAddress == null)
            {
                contractAddress = CommandOption.InputParameters(1)[0];
            }

            $"Contract: {input[0]}, Address: {contractAddress}".WriteWarningLine();
            contractInfo.GetContractMethodsInfo();

            var methodEngine = new CommandsCompletionEngine(contractInfo.ActionMethodNames);
            var methodReader = new ConsoleReader(methodEngine);
            var methodInput = CommandOption.InputParameters(1, methodReader);

            //method info
            var methodInfo = contractInfo.GetContractMethod(methodInput[0]);
            methodInfo.GetMethodDescriptionInfo();
            methodInfo.GetInputParameters();
            methodInfo.GetOutputParameters();

            var parameterInput = methodInfo.InputType.Name == "Empty" ? new[] {""} : 
                CommandOption.InputParameters(methodInfo.InputFields.Count);

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

            var inputMessage = JsonParser.Default.Parse(jsonObject.ToString(), methodInfo.InputType);
            var transactionId = NodeManager.SendTransaction(Services.Genesis.CallAddress, contractAddress,
                methodInput[0], inputMessage);
            var transactionResult = NodeManager.CheckTransactionResult(transactionId);
            Logger.Info(JsonConvert.SerializeObject(transactionResult, Formatting.Indented));
        }

        public override CommandInfo GetCommandInfo()
        {
            return new CommandInfo
            {
                Name = "send",
                Description = "Execute contract action methods"
            };
        }

        public override string[] InputParameters()
        {
            throw new NotImplementedException();
        }
    }
}