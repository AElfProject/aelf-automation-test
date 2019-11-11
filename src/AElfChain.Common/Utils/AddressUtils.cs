using System;
using AElf.Automation.Common.Helpers;
using AElf.Types;

namespace AElf.Automation.Common.Utils
{
    public static class AddressUtils
    {
        public static Address Generate()
        {
            var rd = new Random(Guid.NewGuid().GetHashCode());
            var randomBytes = new byte[32];
            rd.NextBytes(randomBytes);

            return Address.FromBytes(randomBytes);
        }

        public static Address ConvertAddress(this string address)
        {
            try
            {
                return AddressHelper.Base58StringToAddress(address);
            }
            catch (Exception e)
            {
                $"Convert '{address}' to Address type got error. Error: {e.Message}".WriteErrorLine();
                throw;
            }
        }
    }
}