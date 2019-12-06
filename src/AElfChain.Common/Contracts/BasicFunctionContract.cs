using System.Linq;
using AElf.Contracts.TestContract.BasicFunction;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;

namespace AElfChain.Common.Contracts
{
    public enum FunctionMethod
    {
        InitialBasicFunctionContract,
        UpdateBetLimit,
        UserPlayBet,

        GetContractName,
        QueryWinMoney,
        QueryRewardMoney,
        QueryUserWinMoney,
        QueryUserLoseMoney
    }

    public class BasicFunctionContract : BaseContract<FunctionMethod>
    {
        public BasicFunctionContract(INodeManager nodeManager, string callAddress, string contractAddress)
            : base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }

        public BasicFunctionContract(INodeManager nodeManager, string callAddress)
            : base(nodeManager, ContractFileName, callAddress)
        {
        }

        public void InitialBasicFunctionContract()
        {
            ExecuteMethodWithResult(FunctionMethod.InitialBasicFunctionContract,
                new InitialBasicContractInput
                {
                    ContractName = "Test Contract1",
                    MinValue = 10L,
                    MaxValue = 1000L,
                    MortgageValue = 1000_000_000L,
                    Manager = CallAccount
                });

            SetAccount(CallAccount.GetFormatted());
            ExecuteMethodWithResult(FunctionMethod.UpdateBetLimit, new BetLimitInput
            {
                MinValue = 50L,
                MaxValue = 100_0000L
            });
        }

        public string GetContractName()
        {
            return CallViewMethod<StringValue>(FunctionMethod.GetContractName, new Empty()).Value;
        }

        public static BasicFunctionContract GetOrDeployBasicFunctionContract(INodeManager nodeManager, string callAddress)
        {
            var genesis = nodeManager.GetGenesisContract();
            var addressList = genesis.QueryCustomContractByMethodName("InitialBasicFunctionContract");
            if (addressList.Count == 0)
            {
                var contract = new BasicFunctionContract(nodeManager, callAddress);
                contract.InitialBasicFunctionContract();
                return contract;
            }

            return new BasicFunctionContract(nodeManager, callAddress, addressList.First().GetFormatted());
        }

        public static string ContractFileName => "AElf.Contracts.TestContract.BasicFunction";
    }
}