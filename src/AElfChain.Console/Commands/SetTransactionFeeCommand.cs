using Acs1;
using AElf.Types;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using AElfChain.Console.InputOption;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using Shouldly;

namespace AElfChain.Console.Commands
{
    public class SetTransactionFeeCommand : BaseCommand
    {
        public SetTransactionFeeCommand(INodeManager nodeManager, ContractManager contractManager)
            : base(nodeManager, contractManager)
        {
        }

        public override void RunCommand()
        {
            var parameters = InputParameters();
            if (parameters == null)
                return;

            var caller = Services.Token.CallAddress;
            switch (parameters.Length)
            {
                case 2:
                {
                    var feeResult = NodeManager.QueryView<MethodFees>(Services.Genesis.CallAddress, parameters[0],
                        "GetMethodFee", new StringValue
                        {
                            Value = parameters[1]
                        });
                    Logger.Info(JsonConvert.SerializeObject(feeResult, Formatting.Indented));
                    break;
                }
                case 4:
                {
                    var input = new MethodFees
                    {
                        MethodName = parameters[1],
                        Fees =
                        {
                            new MethodFee
                            {
                                Symbol = parameters[2],
                                BasicFee = long.Parse(parameters[3])
                            }
                        }
                    };
                    var genesisOwner = Services.Authority.GetGenesisOwnerAddress();
                    var miners = Services.Authority.GetCurrentMiners();
                    var transactionResult = Services.Authority.ExecuteTransactionWithAuthority(parameters[0],
                        "SetMethodFee",
                        input, genesisOwner, miners, caller);
                    transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                    //query result
                    var methodInput = new StringValue
                    {
                        Value = parameters[1]
                    };
                    var tokenResult = NodeManager.QueryView<MethodFees>(caller, parameters[0], "GetMethodFee",
                        methodInput);
                    $"MethodFee: {tokenResult}".WriteSuccessLine();
                    break;
                }
                default:
                    "Wrong input parameters.".WriteErrorLine();
                    return;
            }
        }

        public override CommandInfo GetCommandInfo()
        {
            return new CommandInfo
            {
                Name = "tx-fee",
                Description = "Get/Set transaction method fee"
            };
        }

        public override string[] InputParameters()
        {
            var contract = "2ERBTcqx8CzgMP7fvQS4DnKQX1AM98CSwAGFyRCQvn9Bvs4Qt1";
            var method = "Approve";
            var symbol = "TELF";
            var amount = 1000;

            "Parameter: [ContractAddress] [Method] [Symbol] [Amount]".WriteSuccessLine();
            $"eg-[GET]: {contract} {method}".WriteSuccessLine();
            $"eg-[SET]: {contract} {method} {symbol} {amount}".WriteSuccessLine();
            var reader = new ConsoleReader(new ContractsCompletionEngine(Services.SystemContracts));
            return CommandOption.InputParameters(2, reader);
        }
    }
}