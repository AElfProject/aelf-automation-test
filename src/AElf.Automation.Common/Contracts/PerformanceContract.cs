using System;
using AElf.Automation.Common.Helpers;
using AElfChain.SDK.Models;
using AElf.Contracts.TestContract.Performance;
using AElf.Types;
using Shouldly;

namespace AElf.Automation.Common.Contracts
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
        public PerformanceContract(IApiHelper apiHelper, string callAddress, string contractAddress) 
            : base(apiHelper, contractAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }

        public PerformanceContract(IApiHelper apiHelper, string callAddress) 
            : base(apiHelper, ContractFileName, callAddress)
        {
        }

        public void InitializePerformance()
        {
            var initializeResult = ExecuteMethodWithResult(PerformanceMethod.InitialPerformanceContract, new InitialPerformanceInput
            {
                ContractName = $"Performance_{Guid.NewGuid().ToString()}",
                Manager = AddressHelper.Base58StringToAddress(CallAddress)
            });
            if (initializeResult.InfoMsg is TransactionResultDto txDto)
            {
                txDto.Status.ConvertTransactionResultStatus().ShouldBe(TransactionResultStatus.Mined);
            }
        }
        
        public static string ContractFileName => "AElf.Contracts.TestContract.Performance";
    }
}