using System;
using System.Linq;
using AElfChain.Common.Contracts;
using AElfChain.Common.Contracts.Serializer;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElfChain.Console.InputOption;
using Google.Protobuf;

namespace AElfChain.Console.Commands
{
    public class ContractQueryCommand : BaseCommand
    {
        public ContractQueryCommand(INodeManager nodeManager, ContractManager contractManager)
            : base(nodeManager, contractManager)
        {
            Logger = Log4NetHelper.GetLogger();
            ContractSerializer = new ContractSerializer();
        }

        private ContractSerializer ContractSerializer { get; }

        public override void RunCommand()
        {
            "Select system contract name: ".WriteSuccessLine();
            var contractEngine = new CommandsCompletionEngine(ContractSerializer.SystemContractsDescriptors.Keys
                .Select(o => o.ToString()).ToList());
            var contractReader = new ConsoleReader(contractEngine);
            var input = CommandOption.InputParameters(1, contractReader);
            if (input == null)
                return;
            var nameProvider = input[0].ConvertNameProvider();
            var contractInfo = ContractSerializer.GetContractInfo(nameProvider);
            string contractAddress;
            if (input.Length == 2)
                contractAddress = input[1];
            else
                contractAddress = Services.GetContractAddress(input[0]) ??
                                  CommandOption.InputParameters(1, "Input contract address")[0];
            $"Contract: {input[0]}, Address: {contractAddress}".WriteWarningLine();
            contractInfo.GetContractViewMethodsInfo();

            var methodEngine = new CommandsCompletionEngine(contractInfo.ViewMethodNames);
            var methodReader = new ConsoleReader(methodEngine);
            while (true)
            {
                var methodInput = CommandOption.InputParameters(1, methodReader);
                if (methodInput[0] == "exit")
                    break;

                //method info
                var methodInfo = contractInfo.GetContractMethod(methodInput[0]);
                methodInfo.GetMethodDescriptionInfo();
                methodInfo.GetInputParameters();
                methodInfo.GetOutputParameters();

                var parameterInput = methodInfo.InputType.Name == "Empty"
                    ? new[] {""}
                    : CommandOption.InputParameters(methodInfo.InputFields.Count);
                var jsonInfo = methodInfo.ParseMethodInputJsonInfo(parameterInput);
                var inputMessage = JsonParser.Default.Parse(jsonInfo, methodInfo.InputType);
                var byteString = NodeManager.QueryView(Services.Genesis.CallAddress, contractAddress,
                    methodInput[0],
                    inputMessage);
                var message = methodInfo.OutputType.Parser.ParseFrom(byteString);
                Logger.Info(JsonFormatter.ToDiagnosticString(message), Format.Json);
            }
        }

        public override CommandInfo GetCommandInfo()
        {
            return new CommandInfo
            {
                Name = "call",
                Description = "Call contract view methods"
            };
        }

        public override string[] InputParameters()
        {
            throw new NotImplementedException();
        }
    }
}