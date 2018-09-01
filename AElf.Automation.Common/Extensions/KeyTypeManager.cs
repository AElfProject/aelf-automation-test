using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Automation.Common.Helpers;
using AElf.Common.ByteArrayHelpers;
using AElf.Common.Extensions;
using AElf.Kernel;
using AElf.SmartContract;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using NLog.Fluent;
using HashType = AElf.Kernel.HashType;

namespace AElf.Automation.Common.Extensions
{
    public class KeyInfo
    {
        public string RedisKey { get; set; }
        public byte[] RedisValue { get; set; }
        public string BasicString { get; set; }
        public string HashString { get; set; }
        public int KeyLength { get; set; }
        public int ValueLength { get; set; }
        public Key KeyObject { get; set; }
        public Object ValueInfo { get; set; }
        public bool Checked { get; set; }

        public KeyInfo(string key)
        {
            RedisKey = key;
            RedisValue = new byte[]{};
            BasicString = string.Empty;
            HashString = string.Empty;
            KeyLength = 0;
            ValueLength = 0;
            KeyObject = null;
            ValueInfo = new Object();
            Checked = false;
        }

        public override string ToString()
        {
            return $"KeyType:{BasicString}, HashType:{HashString}, RedisKey:{RedisKey}, Length:[{KeyLength},{ValueLength}], ObjectValue:{ValueInfo.ToString()}";
        }
    }
    public class KeyTypeManager
    {
        public List<KeyInfo> InfoCollection { get; set; }
        public Dictionary<string, List<KeyInfo>> HashList { get; set; }
        public Dictionary<string, List<KeyInfo>> ProtoHashList { get; set; }
        public RedisHelper RH { get; set; }
        public List<string> KeyList { get; set; }
        public ILogHelper Logger = LogHelper.GetLogHelper();

        public KeyTypeManager(RedisHelper redisHelper)
        {
            InfoCollection = new List<KeyInfo>();
            HashList = new Dictionary<string, List<KeyInfo>>();
            ProtoHashList = new Dictionary<string, List<KeyInfo>>();
            RH = redisHelper;
            KeyList = RH.GetAllKeys();
        }

        public KeyInfo GetKeyInfo(string key)
        {
            var keyInfo = new KeyInfo(key);

            try
            {
                byte[] info = ByteArrayHelpers.FromHexString(key);
                keyInfo.RedisValue = info;
                keyInfo.KeyLength = info.Length;
                ProtobufSerializer ps = new ProtobufSerializer();
                Key objectKey = ps.Deserialize<Key>(info);
                keyInfo.KeyObject = objectKey;
                string typeStr = objectKey.Type;
                keyInfo.BasicString = typeStr;
                int valueLength = 0;
                keyInfo.ValueInfo = ConvertKeyValue(objectKey, out valueLength);
                keyInfo.ValueLength = valueLength;
                if (typeStr == "Hash")
                {
                    var hashType = (HashType) objectKey.HashType;
                    keyInfo.HashString = hashType.ToString();
                }
                else
                    keyInfo.HashString = string.Empty;

                if(!HashList.ContainsKey(typeStr))
                    HashList.Add(typeStr, new List<KeyInfo>());
                HashList[typeStr].Add(keyInfo);
            }
            catch (Exception)
            {
                Logger.WriteError($"Get key info exception: {key}");
            }

            return keyInfo;
        }

        public void GetAllKeyInfoCollection()
        {
            foreach (var item in KeyList)
            {
                InfoCollection.Add(GetKeyInfo(item));
            }
        }

        public void PrintSummaryInfo(bool detail=true)
        {
            int totalCount = InfoCollection.Sum(x => x.KeyLength + x.ValueLength);
            Logger.WriteInfo($"Total keys count:{InfoCollection.Count.ToString()}, Bytes total length:{totalCount.ToString()}");
            Logger.WriteInfo("All keys type info:");
            foreach (var key in HashList.Keys)
            {
                //Keys Percent info
                int itemCount = HashList[key].Sum(x => x.KeyLength + x.ValueLength);
                double percent = (double) (itemCount*100) / (double) totalCount;
                Logger.WriteInfo($"Key item: {key}, Count: {HashList[key].Count.ToString()}, Percent:{percent:0.00}%");
            }

            Logger.WriteInfo("All hash keys type info:");
            ConvertHashType();
            foreach (var key in ProtoHashList.Keys)
            {
                Logger.WriteInfo($"Key item: {key}, Count: {ProtoHashList[key].Count.ToString()}");
            }

            if (!detail)
                return;

            //打印Basic信息
            var sortList = InfoCollection.OrderBy(o=>o.BasicString).ThenBy(o=>o.HashString).ToList();
            foreach (var item in sortList)
            {
                Logger.WriteInfo($"BasicCategory={item.BasicString}, HashCategory={item.HashString}, Length=[{item.KeyLength},{item.ValueLength}], RedisKey={item.RedisKey}");
            }
            //打印Object信息
            foreach (var item in HashList.Keys)
            {
                Logger.WriteInfo("------------------------------------------------------------------------------------");
                Logger.WriteInfo($"Data Type: {item}");
                foreach (var keyinfo in HashList[item])
                {
                    Logger.WriteInfo(keyinfo.ValueInfo.ToString());
                }
                Logger.WriteInfo("------------------------------------------------------------------------------------");
            }
        }

        private Object ConvertKeyValue(Key key, out int length)
        {
            string keyStr = key.ToByteArray().ToHex();
            byte[] keyValue = RH.GetT<byte[]>(keyStr);
            length = keyValue.Length;
            ProtobufSerializer ps = new ProtobufSerializer();
            string keyType = key.Type;

            Object returnObj = new Object();
            switch (keyType)
            {
                case "UInt64Value":
                    returnObj = ps.Deserialize<UInt64Value>(keyValue);
                    break;
                case "Hash":
                    returnObj = ps.Deserialize<Hash>(keyValue);
                    break;
                case "BlockBody":
                    returnObj = ps.Deserialize<BlockBody>(keyValue);
                    break;
                case "BlockHeader":
                    returnObj = ps.Deserialize<BlockHeader>(keyValue);
                    break;
                case "Chain":
                    returnObj = ps.Deserialize<Chain>(keyValue);
                    break;
                case "Change":
                    break;
                case "SmartContractRegistration":
                    returnObj = ps.Deserialize<SmartContractRegistration>(keyValue);
                    break;
                case "TransactionResult":
                    returnObj = ps.Deserialize<TransactionResult>(keyValue);
                    break;
                case "Transaction":
                    returnObj = ps.Deserialize<Transaction>(keyValue);
                    break;
                case "FunctionMetadata":
                    returnObj = ps.Deserialize<FunctionMetadata>(keyValue);
                    break;
                case "SerializedCallGraph":
                    returnObj = ps.Deserialize<SerializedCallGraph>(keyValue);
                    break;
                case "SideChain":
                    returnObj = ps.Deserialize<SideChain>(keyValue);
                    break;
                case "WorldState":
                    returnObj = ps.Deserialize<WorldState>(keyValue);
                    break;
                case "Miners":
                    returnObj = ps.Deserialize<Miners>(keyValue);
                    break;
                case "BlockProducer":
                    returnObj = ps.Deserialize<BlockProducer>(keyValue);
                    break;
                case "Round":
                    returnObj = ps.Deserialize<Round>(keyValue);
                    break;
                case "AElfDPoSInformation":
                    returnObj = ps.Deserialize<AElfDPoSInformation>(keyValue);
                    break;
                case "Int32Value":
                    returnObj = ps.Deserialize<Int32Value>(keyValue);
                    break;
                case "StringValue":
                    returnObj = ps.Deserialize<StringValue>(keyValue);
                    break;
                case "Timestamp":
                    returnObj = ps.Deserialize<Timestamp>(keyValue);
                    break;
                case "SInt32Value":
                    returnObj = ps.Deserialize<SInt32Value>(keyValue);
                    break;
                default:
                    break;
            }

            return returnObj;
        }

        public void ConvertHashType()
        {
            if (!HashList.ContainsKey("Hash"))
                return;
            ProtoHashList = new Dictionary<string, List<KeyInfo>>();

            foreach (var item in HashList?["Hash"])
            {
                try
                {
                    var type = (HashType)item.KeyObject.HashType;
                    string typeStr = type.ToString();
                    if(!ProtoHashList.ContainsKey(typeStr))
                        ProtoHashList.Add(typeStr, new List<KeyInfo>());
                    ProtoHashList[typeStr].Add(item);
                }
                catch (Exception e)
                {
                    Logger.WriteInfo($"Convert hash key exception: {item}");
                }
            }
        }
    }
}