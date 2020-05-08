using System.Linq;
using AElf.Contracts.TestContract.BasicFunction;
using AElf.Types;
using AElfChain.Common.DtoExtension;
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

        public static string ContractFileName => "AElf.Contracts.TestContract.BasicFunction";

        public void InitialBasicFunctionContract()
        {
            var initializeResult = ExecuteMethodWithResult(FunctionMethod.InitialBasicFunctionContract,
                new InitialBasicContractInput
                {
                    ContractName = "Test Contract1",
                    MinValue = 10L,
                    MaxValue = 10_0000L,
                    MortgageValue = 1000_000_000L,
                    Manager = CallAccount
                });
            if (initializeResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined)
                Logger.Error(
                    $"Initialize execution of basic function contract failed. Error: {initializeResult.Error}");

            var setLimitResult = ExecuteMethodWithResult(FunctionMethod.UpdateBetLimit, new BetLimitInput
            {
                MinValue = 50L,
                MaxValue = 100_0000L
            });
            if (setLimitResult.Status.ConvertTransactionResultStatus() != TransactionResultStatus.Mined)
                Logger.Error(
                    $"UpdateBetLimit execution of basic function contract failed. Error: {setLimitResult.Error}");
        }

        public string GetContractName()
        {
            return CallViewMethod<StringValue>(FunctionMethod.GetContractName, new Empty()).Value;
        }

        public static BasicFunctionContract GetOrDeployBasicFunctionContract(INodeManager nodeManager,
            string callAddress)
        {
            var genesis = nodeManager.GetGenesisContract();
            var addressList = genesis.QueryCustomContractByMethodName("InitialBasicFunctionContract");
            if (addressList.Count == 0)
            {
                var contract = new BasicFunctionContract(nodeManager, callAddress);
                contract.InitialBasicFunctionContract();
                return contract;
            }

            return new BasicFunctionContract(nodeManager, callAddress, addressList.First().ToBase58());
        }
    }
}