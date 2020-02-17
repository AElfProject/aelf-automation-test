using System;
using System.Linq;
using AElf.Contracts.TestContract.Performance;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;
using Shouldly;

namespace AElfChain.Common.Contracts
{
    public enum PerformanceMethod
    {
        //Action
        InitialPerformanceContract,
        Write1KContentByte,
        Write2KContentByte,
        Write5KContentByte,
        Write10KContentByte,
        ComputeLevel1,
        ComputeLevel2,
        ComputeLevel3,
        ComputeLevel4,

        //View
        QueryReadInfo,
        QueryFibonacci
    }

    public class PerformanceContract : BaseContract<PerformanceMethod>
    {
        public PerformanceContract(INodeManager nodeManager, string callAddress, string contractAddress)
            : base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }

        public PerformanceContract(INodeManager nodeManager, string callAddress)
            : base(nodeManager, ContractFileName, callAddress)
        {
        }

        public static string ContractFileName => "AElf.Contracts.TestContract.Performance";

        public void InitializePerformance()
        {
            var initializeResult = ExecuteMethodWithResult(PerformanceMethod.InitialPerformanceContract,
                new InitialPerformanceInput
                {
                    ContractName = $"Performance_{Guid.NewGuid().ToString()}",
                    Manager = CallAddress.ConvertAddress()
                });
            initializeResult.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
        }

        public static PerformanceContract GetOrDeployPerformanceContract(INodeManager nodeManager, string callAddress)
        {
            var genesis = nodeManager.GetGenesisContract();
            var addressList = genesis.QueryCustomContractByMethodName("InitialPerformanceContract");
            if (addressList.Count == 0)
            {
                var contract = new PerformanceContract(nodeManager, callAddress);
                contract.InitializePerformance();
                return contract;
            }

            return new PerformanceContract(nodeManager, callAddress, addressList.First().GetFormatted());
        }
    }
}