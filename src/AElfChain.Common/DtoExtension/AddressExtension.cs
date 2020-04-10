using System;
using AElf;
using AElf.Types;
using AElfChain.Common.Helpers;

namespace AElfChain.Common.DtoExtension
{
    public static class AddressExtension
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

        public static bool IsAddressInfo(string info, out Address address)
        {
            address = new Address();
            try
            {
                address = info.ConvertAddress();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public static class HashExtension
    {
        public static bool IsHashInfo(string info, out Hash hash)
        {
            hash = new Hash();
            try
            {
                hash = HashHelper.HexStringToHash(info);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}