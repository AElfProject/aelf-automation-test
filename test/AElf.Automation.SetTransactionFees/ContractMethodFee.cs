using System.Collections.Generic;
using System.Linq;
using Acs1;
using AElf.Types;
using AElfChain.Common.Contracts.Serializer;
using AElfChain.Common.Helpers;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;
using log4net;
using Newtonsoft.Json;
using Shouldly;

namespace AElf.Automation.SetTransactionFees
{
    public class ContractMethodFee
    {
        private static readonly ILog Logger = Log4NetHelper.GetLogger();

        private readonly List<string> _noFeeMethods = new List<string>
        {
            "InitialAElfConsensusContract",
            "FirstRound",
            "NextRound",
            "AEDPoSContractStub.NextTerm",
            "UpdateValue",
            "UpdateTinyBlockInformation",
            "ClaimTransactionFees",
            "DonateResourceToken",
            "RecordCrossChainData",
            "ChargeTransactionFees",
            "CheckThreshold",
            "CheckResourceToken",
            "ChargeResourceToken"
        };

        public ContractMethodFee(INodeManager nodeManager, AuthorityManager authority, ContractInfo contract,
            string contractAddress)
        {
            NodeManager = nodeManager;
            Authority = authority;
            Contract = contract;
            ContractAddress = contractAddress;
        }

        private INodeManager NodeManager { get; }
        private AuthorityManager Authority { get; }
        private ContractInfo Contract { get; }
        private string ContractAddress { get; }

        public void SetContractFees(string symbol, long amount, Address organizationAddress, List<string> approveUsers,
            string caller)
        {
            foreach (var method in Contract.Methods)
            {
                if (_noFeeMethods.Contains(method.Name))
                {
                    Logger.Info($"No need to set method fee for: {method.Name}");
                    continue;
                }

                Logger.Info($"Set method fee: {method.Name}");
                //before query
                var beforeFee = QueryTransactionFee(caller, ContractAddress, method.Name);
                if (beforeFee.Fees.Count > 0)
                {
                    var primaryToken = NodeManager.GetPrimaryTokenSymbol();
                    var tokenAmount = beforeFee.Fees.First(o => o.Symbol == primaryToken);
                    if (tokenAmount?.BasicFee == amount)
                    {
                        Logger.Info($"{method.Name} transaction fee is {amount}, no need to reset again.");
                        continue;
                    }
                }

                //set transaction fee
                var input = new MethodFees
                {
                    MethodName = method.Name,
                    Fees =
                    {
                        new MethodFee
                        {
                            Symbol = symbol,
                            BasicFee = amount
                        }
                    }
                };
                var transactionResult = Authority.ExecuteTransactionWithAuthority(ContractAddress, "SetMethodFee",
                    input, organizationAddress,
                    approveUsers, caller);
                transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                //query result
                var afterFee = QueryTransactionFee(caller, ContractAddress, method.Name);
                Logger.Info(JsonConvert.SerializeObject(afterFee, Formatting.Indented));
                afterFee.Fees.First().BasicFee.ShouldBe(amount);
            }
        }

        private MethodFees QueryTransactionFee(string caller, string contract, string method)
        {
            var methodName = new StringValue
            {
                Value = method
            };
            var feeResult = NodeManager.QueryView<MethodFees>(caller, contract, "GetMethodFee", methodName);

            return feeResult;
        }
    }
}