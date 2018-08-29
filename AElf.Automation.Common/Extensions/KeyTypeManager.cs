using System;
using System.Collections.Generic;
using AElf.Automation.Common.Helpers;
using AElf.Common.ByteArrayHelpers;
using AElf.Common.Extensions;
using AElf.Kernel;
using AElf.SmartContract;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using QuickGraph;
using ATypes = AElf.Kernel.Storages.Types;
using HashType = AElf.Kernel.HashType;

namespace AElf.Automation.Common.Extensions
{
    public class KeyTypeManager
    {
        public Dictionary<string, List<Key>> HashList { get; set; }
        public Dictionary<string, List<Key>> ProtoHashList { get; set; }
        public RedisHelper RH { get; set; }
        public ILogHelper Logger = LogHelper.GetLogHelper();


        public KeyTypeManager(RedisHelper redisHelper)
        {
            HashList = new Dictionary<string, List<Key>>();
            ProtoHashList = new Dictionary<string, List<Key>>();
            RH = redisHelper;
        }

        public Key GetHashFromKey(string key)
        {
            try
            {
                byte[] info = ByteArrayHelpers.FromHexString(key);
                ProtobufSerializer ps = new ProtobufSerializer();
                Key objectKey = ps.Deserialize<Key>(info);
                var type = (ATypes) objectKey.Type;
                string typeStr = type.ToString();
                if(!HashList.ContainsKey(typeStr))
                    HashList.Add(typeStr, new List<Key>());
                HashList[typeStr].Add(objectKey);

                return objectKey;
            }
            catch (Exception e)
            {
                Logger.Write($"Convert exception: {key}");
                return null;
            }

        }

        public void ConvertHashType()
        {
            foreach (var item in HashList["Hash"])
            {
                try
                {
                    var type = (Kernel.Storages.Types)item.HashType;
                    string typeStr = type.ToString();
                    if(!ProtoHashList.ContainsKey(typeStr))
                        ProtoHashList.Add(typeStr, new List<Key>());
                    ProtoHashList[typeStr].Add(item);
                }
                catch (Exception e)
                {
                    Logger.Write($"Convert exception: {item}");
                }
            }
        }

        public Object ConvertKeyValue(Key key)
        {
            string keyStr = key.ToByteArray().ToHex();
            byte[] keyValue = RH.GetT<byte[]>(keyStr);
            ProtobufSerializer ps = new ProtobufSerializer();
            var hashType = (ATypes)key.Type;
            Object returnObj = new Object();
            switch (hashType)
            {
                case ATypes.UInt64Value:
                    returnObj = ps.Deserialize<UInt64Value>(keyValue);
                    break;
                case ATypes.Hash:
                    returnObj = ps.Deserialize<Hash>(keyValue);
                    break;
                case ATypes.BlockBody:
                    returnObj = ps.Deserialize<BlockBody>(keyValue);
                    break;
                case ATypes.BlockHeader:
                    returnObj = ps.Deserialize<BlockHeader>(keyValue);
                    break;
                case ATypes.Chain:
                    returnObj = ps.Deserialize<Chain>(keyValue);
                    break;
                case ATypes.Change:
                    break;
                case ATypes.SmartContractRegistration:
                    returnObj = ps.Deserialize<SmartContractRegistration>(keyValue);
                    break;
                case ATypes.TransactionResult:
                    returnObj = ps.Deserialize<TransactionResult>(keyValue);
                    break;
                case ATypes.Transaction:
                    returnObj = ps.Deserialize<Transaction>(keyValue);
                    break;
                case ATypes.FunctionMetadata:
                    returnObj = ps.Deserialize<FunctionMetadata>(keyValue);
                    break;
                case ATypes.SerializedCallGraph:
                    returnObj = ps.Deserialize<SerializedCallGraph>(keyValue);
                    break;
                case ATypes.SideChain:
                    returnObj = ps.Deserialize<SideChain>(keyValue);
                    break;
                case ATypes.WorldState:
                    returnObj = ps.Deserialize<WorldState>(keyValue);
                    break;
                case ATypes.Miners:
                    returnObj = ps.Deserialize<Miners>(keyValue);
                    break;
                case ATypes.BlockProducer:
                    returnObj = ps.Deserialize<BlockProducer>(keyValue);
                    break;
                case ATypes.Round:
                    returnObj = ps.Deserialize<Round>(keyValue);
                    break;
                case ATypes.AElfDPoSInformation:
                    returnObj = ps.Deserialize<AElfDPoSInformation>(keyValue);
                    break;
                case ATypes.Int32Value:
                    returnObj = ps.Deserialize<Int32Value>(keyValue);
                    break;
                case ATypes.StringValue:
                    returnObj = ps.Deserialize<StringValue>(keyValue);
                    break;
                case ATypes.Timestamp:
                    returnObj = ps.Deserialize<Timestamp>(keyValue);
                    break;
                case ATypes.SInt32Value:
                    returnObj = ps.Deserialize<SInt32Value>(keyValue);
                    break;
                default:
                    break;
            }

            return returnObj;
        }

        public void PrintSummaryInfo()
        {
            Logger.Write("All keys type info:");
            foreach (var key in HashList.Keys)
            {
                Logger.Write($"Key item: {key}, Count: {HashList[key].Count}");
                }

            Logger.Write("All hash keys type info:");
            foreach (var key in ProtoHashList.Keys)
            {
                Logger.Write($"Key item: {key}, Count: {ProtoHashList[key].Count}");
            }
        }
    }
}