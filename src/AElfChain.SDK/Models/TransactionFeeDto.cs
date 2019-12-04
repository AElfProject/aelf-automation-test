using System.Collections.Generic;
using System.Linq;
using AElf.Types;
using Google.Protobuf.Collections;

namespace AElfChain.SDK.Models
{
    public class TransactionFeeDto
    {
        public Dictionary<string, long> Value { get; set; }

        public string GetTransactionFeeInfo()
        {
            if (Value == null)
                return "Fee : 0";
            else
            {
                var feeInfo = "Fee: ";
                foreach (var key in Value.Keys)
                {
                    feeInfo += $"{key}={Value[key]} ";
                }

                return feeInfo.Trim();
            }
        }

        public long GetDefaultTransactionFee()
        {
            return Value?.Values.First() ?? 0;
        }
    }

    public static class TransactionFeeExtension
    {
        public static long GetDefaultTransactionFee(this TransactionFee transactionFee)
        {
            if (transactionFee == null) return 0;
            return transactionFee.Value.Values.First();
        }

        public static TransactionFee ConvertTransactionFeeDto(this TransactionFeeDto feeDto)
        {
            if (feeDto == null) return null;
            var values = new MapField<string, long>();
            foreach (var key in feeDto.Value.Keys)
            {
                values.Add(key, feeDto.Value[key]);
            }

            return new TransactionFee
            {
                Value = {values}
            };
        }
    }
}