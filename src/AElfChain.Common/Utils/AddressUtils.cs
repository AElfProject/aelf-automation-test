using System;
using AElf;
using AElf.Types;

namespace AElfChain.Common.Utils
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
            return AddressHelper.Base58StringToAddress(address);
        }
    }
}