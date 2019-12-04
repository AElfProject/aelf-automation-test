using System.Collections.Generic;
using System.Linq;

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
}