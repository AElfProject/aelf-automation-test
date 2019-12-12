using System.Linq;
using AElf.Client.Dto;
using AElf.Types;
using Google.Protobuf.Collections;

namespace AElfChain.Common.DtoExtension
{
    public static class TransactionFeeExtension
    {
        public static string GetTransactionFeeInfo(this TransactionFeeDto feeDto)
        {
            if (feeDto.Value == null)
                return "Fee=0";
            
            var feeInfo = "Fee: ";
            foreach (var key in feeDto.Value.Keys)
            {
                feeInfo += $"{key}={feeDto.Value[key]} ";
            }

            return feeInfo.Trim();
        }
        
        public static long GetDefaultTransactionFee(this TransactionFeeDto transactionFee)
        {
            if (transactionFee == null) return 0;
            return transactionFee.Value.Values.First();
        }

        public static TransactionFee ConvertTransactionFeeDto(this TransactionFeeDto feeDto)
        {
            if (feeDto == null) return null;
            var values = new MapField<string, long>();
            if (feeDto.Value != null)
            {
                foreach (var key in feeDto.Value.Keys)
                {
                    values.Add(key, feeDto.Value[key]);
                }
            }

            return new TransactionFee
            {
                Value = {values}
            };
        }

        public static long GetDefaultTransactionFee(this TransactionFee fee)
        {
            return fee.Value?.Values.First() ?? 0;
        }
    }
}