using System;
using ProtoBuf;
using AElf.Common;

namespace AElf.Automation.Common.Protobuf
{
    [ProtoContract]
    public class Address
    {
        public Address(byte[] val)
        {
            Value = val;
        }

        [ProtoMember(1)]
        public byte[] Value { get; set; }

        public static implicit operator Address(byte[] value)
        {
            return new Address(value);
        }

        public static Address Parse(string inputStr)
        {
            return new Address(Base58CheckEncoding.Decode(inputStr));
        }

        public string GetFormatted()
        {
            if (Value.Length != TypeConsts.AddressHashLength)
            {
                throw new ArgumentOutOfRangeException(
                    $"Serialized value does not represent a valid address. The input is {Value.Length} bytes long.");
            }

            return Base58CheckEncoding.Encode(Value);
        }
    }
    
    [ProtoContract]
    public class Hash
    {
        public Hash(byte[] val)
        {
            Value = val;
        }

        [ProtoMember(1)]
        public byte[] Value { get; set; }

        public static implicit operator Hash(byte[] value)
        {
            return new Hash(value);
        }
    }
}
