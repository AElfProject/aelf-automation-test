using System;
using System.ServiceModel.Channels;
using AElf;
using Microsoft.Extensions.CommandLineUtils;

namespace AElfChain.Console.CommandOptions
{
    public static class CommandOptionExtensions
    {
        public static string TryParseRequiredString(this CommandOption option, bool hasInputErrors)
        {
            if (!string.IsNullOrWhiteSpace(option.Value())) return option.Value();
            
            System.Console.WriteLine(option.ShortName + "|" + option.LongName + " has not been set");
            hasInputErrors = true;
            return null;
        }
        
        public static string TryParseAndValidateAddress(this CommandOption option, bool hasInputErrors, bool required = true)
        {
            var value = option.Value();
            if (required)
                value = TryParseRequiredString(option, true);

            if (string.IsNullOrEmpty(value)) return null;
            
            try
            {
                AddressHelper.Base58StringToAddress(value);
            }
            catch (Exception e)
            {
                System.Console.WriteLine(option.ShortName + "|" + option.LongName + ": The address should have 32 characters in length");
                hasInputErrors = true;
            }
               
            return value;

        }

        public static int? TryParseAndValidateInt(this CommandOption option, bool hasInputErrors, bool required = true)
        {
            var value = option.Value();
            if (required)
                value = TryParseRequiredString(option, hasInputErrors);

            if (string.IsNullOrEmpty(value)) return null;

            var passed = int.TryParse(value, out var intValue);
            if (!passed)
            {
                System.Console.WriteLine(option.ShortName + "|" + option.LongName + " is not a valid integer");
                return null;
            }

            return intValue;
        }
        
        public static long TryParseAndValidateLong(this CommandOption option, bool hasInputErrors, bool required = true)
        {
            var value = option.Value();
            if (required)
                value = TryParseRequiredString(option, hasInputErrors);

            if (string.IsNullOrEmpty(value)) return 0;

            var passed = long.TryParse(value, out var intValue);
            if (!passed)
            {
                System.Console.WriteLine(option.ShortName + "|" + option.LongName + " is not a valid integer");
                return 0;
            }

            return intValue;
        }

        public static bool TryParseAndValidateBool(this CommandOption option, bool hasInputErrors, bool required = false)
        {
            var value = option.Value();
            
            if (string.IsNullOrEmpty(value)) return false;

            var passed = bool.TryParse(value, out var boolValue);
            return passed && boolValue;
        }
    }
}