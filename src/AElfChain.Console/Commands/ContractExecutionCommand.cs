using System;
using System.Linq;
using AElfChain.Common.Contracts;
using AElfChain.Common.ContractSerializer;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElfChain.Console.InputOption;
using Google.Protobuf;
using Newtonsoft.Json;

namespace AElfChain.Console.Commands
{
    public class ContractExecutionCommand : BaseCommand
    {
        public ContractExecutionCommand(INodeManager nodeManager, ContractServices contractServices)
            : base(nodeManager, contractServices)
        {
            Logger = Log4NetHelper.GetLogger();
            ContractHandler = new ContractHandler();
        }

        private ContractHandler ContractHandler { get; }

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
            string contractAddress;
            if (input.Length == 2)
                contractAddress = input[1];
            else
                contractAddress = Services.GetContractAddress(input[0]) ?? CommandOption.InputParameters(1)[0];

            $"Contract: {input[0]}, Address: {contractAddress}".WriteWarningLine();
            contractInfo.GetContractMethodsInfo();

            var methodEngine = new CommandsCompletionEngine(contractInfo.ActionMethodNames);
            var methodReader = new ConsoleReader(methodEngine);
            var sender = Services.Genesis.CallAddress;
            while (true)
            {
                var methodInput = CommandOption.InputParameters(1, methodReader);
                if (methodInput[0] == "exit") break;
                //set sender
                if (methodInput.Length == 2)
                    sender = methodInput[1];
                $"Sender: {sender}".WriteWarningLine();

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
                var transactionId = NodeManager.SendTransaction(sender, contractAddress,
                    methodInput[0], inputMessage, out var existed);
                if (existed)
                {
                    $"TransactionId: {transactionId}, Method: {methodInput[0]}".WriteSuccessLine();
                    return;
                }

                var transactionResult = NodeManager.CheckTransactionResult(transactionId);
                JsonConvert.SerializeObject(transactionResult, Formatting.Indented).WriteSuccessLine();
            }
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