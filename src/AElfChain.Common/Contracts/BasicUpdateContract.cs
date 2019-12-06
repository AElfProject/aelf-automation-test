using System.Linq;
using AElf.Contracts.TestContract.BasicUpdate;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;

namespace AElfChain.Common.Contracts
{
    public enum UpdateMethod
    {
        InitialBasicUpdateContract,
        UpdateBetLimit,
        UserPlayBet,
        UpdateMortgage,
        UpdateStopBet,

        GetContractName,
        QueryWinMoney,
        QueryRewardMoney,
        QueryUserWinMoney,
        QueryUserLoseMoney,
        QueryBetStatus
    }

    public class BasicUpdateContract : BaseContract<UpdateMethod>
    {
        public BasicUpdateContract(INodeManager nodeManager, string callAddress, string contractAddress)
            : base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }

        public BasicUpdateContract(INodeManager nodeManager, string callAddress)
            : base(nodeManager, ContractFileName, callAddress)
        {
        }

        public void InitialBasicUpdateContract()
        {
            ExecuteMethodWithResult(UpdateMethod.InitialBasicUpdateContract,
                new InitialBasicContractInput
                {
                    ContractName = "Test Contract1",
                    MinValue = 10L,
                    MaxValue = 10_0000L,
                    MortgageValue = 1000_000_000L,
                    Manager = CallAccount
                });

            SetAccount(CallAccount.GetFormatted());
            ExecuteMethodWithResult(UpdateMethod.UpdateBetLimit, new BetLimitInput
            {
                MinValue = 50L,
                MaxValue = 100_0000L
            });
        }
        
        public string GetContractName()
        {
            return CallViewMethod<StringValue>(UpdateMethod.GetContractName, new Empty()).Value;
        }
        
        public static BasicUpdateContract GetOrDeployBasicUpdateContract(INodeManager nodeManager, string callAddress)
        {
            var genesis = nodeManager.GetGenesisContract();
            var addressList = genesis.QueryCustomContractByMethodName("InitialBasicUpdateContract");
            if (addressList.Count == 0)
            {
                var contract = new BasicUpdateContract(nodeManager, callAddress);
                contract.InitialBasicUpdateContract();
                return contract;
            }

            return new BasicUpdateContract(nodeManager, callAddress, addressList.First().GetFormatted());
        }

        public static string ContractFileName => "AElf.Contracts.TestContract.BasicUpdate";
    }
}