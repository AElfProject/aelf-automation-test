using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.Client.Dto;
using AElf.Contracts.MultiToken;
using AElf.Types;
using Google.Protobuf;
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
            foreach (var key in feeDto.Value.Keys) feeInfo += $"{key}={feeDto.Value[key]} ";

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
                foreach (var key in feeDto.Value.Keys)
                    values.Add(key, feeDto.Value[key]);

            return new TransactionFee
            {
                Value = {values}
            };
        }

        public static long GetDefaultTransactionFee(this TransactionFee fee)
        {
            return fee.Value?.Values.First() ?? 0;
        }

        public static Dictionary<string, long> GetResourceTokenFee(this TransactionResult transactionResult)
        {
            var dic = new Dictionary<string, long>();
            var eventLogs = transactionResult.Logs;
            foreach (var log in eventLogs)
            {
                if (log.Name == "ResourceTokenCharged")
                {
                    var info = TransactionFeeCharged.Parser.ParseFrom(log.NonIndexed);
                    dic.Add(info.Symbol, info.Amount);
                }    
            }
            
            return dic;
        }
        
        public static Dictionary<string, long> GetResourceTokenFee(this TransactionResultDto transactionResultDto)
        {
            var dic = new Dictionary<string, long>();
            var eventLogs = transactionResultDto.Logs;
            foreach (var log in eventLogs)
            {
                if (log.Name == "ResourceTokenCharged")
                {
                    var info = TransactionFeeCharged.Parser.ParseFrom(ByteString.FromBase64(log.NonIndexed));
                    dic.Add(info.Symbol, info.Amount);
                }    
            }
            
            return dic;
        }
        
        public static (string, long) GetTransactionFee(this TransactionResult transactionResult)
        {
            var eventLogs = transactionResult.Logs;
            foreach (var log in eventLogs)
            {
                if (log.Name == "TransactionFeeCharged")
                {
                    var info = TransactionFeeCharged.Parser.ParseFrom(log.NonIndexed);
                    return (info.Symbol, info.Amount);
                }    
            }
            
            return ("ELF", 0);
        }

        public static (string, long) GetTransactionFee(this TransactionResultDto transactionResultDto)
        {
            var eventLogs = transactionResultDto.Logs;
            foreach (var log in eventLogs)
            {
                if (log.Name == "TransactionFeeCharged")
                {
                    var info = TransactionFeeCharged.Parser.ParseFrom(ByteString.FromBase64(log.NonIndexed));
                    return (info.Symbol, info.Amount);
                }    
            }
            
            return ("ELF", 0);
        }
    }
}