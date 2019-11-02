using System;
using System.Collections.Generic;
using AElf.Types;

namespace AElfChain.SDK.Models
{
    public class TransactionResultDto
    {
        public string TransactionId { get; set; }

        public string Status { get; set; }

        public LogEventDto[] Logs { get; set; }

        public string Bloom { get; set; }

        public long BlockNumber { get; set; }

        public string BlockHash { get; set; }

        public TransactionDto Transaction { get; set; }

        public string ReturnValue { get; set; }
        public string ReadableReturnValue { get; set; }
        
        public string Error { get; set; }
        
        public TransactionFeeDto TransactionFee { get; set; }
    }

    public static class TransactionResultStatusExtension
    {
        public static TransactionResultStatus ConvertTransactionResultStatus(this string status)
        {
            return (TransactionResultStatus) Enum.Parse(typeof(TransactionResultStatus), status, true);
        }
    }
}