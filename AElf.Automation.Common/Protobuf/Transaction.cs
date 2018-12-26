using ProtoBuf;
using System;
using System.Collections.Generic;

namespace AElf.Automation.Common.Protobuf
{
    [ProtoContract]
    public class Transaction
    {
        [ProtoMember(1)]
        public Address From { get; set; }
        
        [ProtoMember(2)]
        public Address To { get; set; }

        [ProtoMember(3)]
        public UInt64 RefBlockNumber { get; set; }

        [ProtoMember(4)]
        public byte[] RefBlockPrefix { get; set; }
        
        [ProtoMember(5)]
        public UInt64 IncrementId { get; set; }
        
        [ProtoMember(6)]
        public string MethodName { get; set; }
        
        [ProtoMember(7)]
        public byte[] Params { get; set; }
        
        [ProtoMember(8)]
        public UInt64 Fee { get; set; }
        
        [ProtoMember(9)]
        public List<byte[]> Sigs { get; set; }
        
        [ProtoMember(10)]
        public TransactionType  Type { get; set; }
    }

    [ProtoContract]
    public enum TransactionType
    {
        [ProtoMember(1)]
        ContractTransaction = 0,

        [ProtoMember(2)]
        DposTransaction = 1,

        [ProtoMember(3)]
        CrossChainBlockInfoTransaction = 2,

        [ProtoMember(4)]
        MsigTransaction = 3,

        [ProtoMember(5)]
        ContractDeployTransaction=4
    }
}
