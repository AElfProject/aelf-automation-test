using System;
using AElf.Client.Dto;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Managers;
using Google.Protobuf.WellKnownTypes;

namespace AElfChain.Common.Contracts
{
    public enum TransferWrapperMethod
    {
        ThroughContractTransfer,
        ContractTransfer,
        Initialize,
        GetTokenAddress
    }

    public class TransferWrapperContract : BaseContract<TransferWrapperMethod>
    {
        public TransferWrapperContract(INodeManager nodeManager, string callAddress, string contractAddress) :
            base(nodeManager, contractAddress)
        {
            SetAccount(callAddress);
        }

        public TransferWrapperContract(INodeManager nodeManager, string callAddress)
            : base(nodeManager, ContractFileName, callAddress)
        {
        }
        public static string ContractFileName => "AElf.Contracts.TransferWrapperContract";
        
        public TransactionResultDto Initialize(Address tokenAddress, string account, string password = "")
        {
            var tester = GetNewTester(account);
            var result = tester.ExecuteMethodWithResult(TransferWrapperMethod.Initialize, tokenAddress);
            return result;
        }

        public Address GetTokenAddress()
        {
            return CallViewMethod<Address>(TransferWrapperMethod.GetTokenAddress, new Empty());
        }
    }
 }