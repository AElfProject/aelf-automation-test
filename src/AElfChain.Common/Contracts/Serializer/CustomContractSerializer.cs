/*
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf;
using AElf.Types;
using AElfChain.Common.DtoExtension;
using AElfChain.Common.Helpers;
using Newtonsoft.Json.Linq;

namespace AElfChain.Common.Contracts.Serializer
{
    public class MessageInfo
    {
        public MessageInfo(string name)
        {
            Name = name;
            Fields = new List<string>();
        }

        public MessageInfo(string name, List<string> fields)
        {
            Name = name;
            Fields = fields;
        }

        public string Name { get; set; }
        public List<string> Fields { get; set; }
    }

    public class MethodInfo
    {
        public MethodInfo(string methodName)
        {
            MethodName = methodName;
        }

        public string MethodName { get; set; }
        public MessageInfo InputMessage { get; set; }
        public MessageInfo OutputMessage { get; set; }
    }

    public class ContractDescriptor
    {
        public ContractDescriptor()
        {
            MessageInfos = new List<MessageInfo>();
            Methods = new List<MethodInfo>();
        }

        public List<MessageInfo> MessageInfos { get; set; }
        public List<MethodInfo> Methods { get; set; }
    }

    public class CustomContractSerializer
    {
        private readonly byte[] _fileDescriptorBytes;

        private readonly List<string> _ignoreProtoFiles = new List<string>
        {
            "google/protobuf/descriptor.proto",
            "google/protobuf/empty.proto",
            "google/protobuf/wrappers.proto"
        };

        private ContractDescriptor _descriptor;

        public CustomContractSerializer(byte[] fileDescriptorBytes)
        {
            _fileDescriptorBytes = fileDescriptorBytes;
        }

        public ContractDescriptor Descriptor => AnalyzeContractDescriptor();

        public ContractDescriptor AnalyzeContractDescriptor()
        {
            if (_descriptor != null)
                return _descriptor;

            var ms = new MemoryStream(_fileDescriptorBytes);
            var descriptorSet = ProtoBuf.Serializer.Deserialize<FileDescriptorSet>(ms);

            _descriptor = new ContractDescriptor();
            foreach (var file in descriptorSet.Files)
            {
                if (_ignoreProtoFiles.Contains(file.Name)) continue;

                var messages = file.MessageTypes;
                if (messages.Count == 0) continue;

                //resolve message info
                foreach (var message in messages)
                {
                    var messageInfo = new MessageInfo(message.Name,
                        message.Fields.Select(o => ConvertNameToJsonName(o.Name)).ToList());
                    _descriptor.MessageInfos.Add(messageInfo);
                }

                //resolve services
                var services = file.Services;
                if (services.Count != 1) continue;
                var methods = services[0].Methods;
                foreach (var method in methods)
                {
                    var methodInfo = new MethodInfo(method.Name);
                    methodInfo.InputMessage = new MessageInfo(method.InputType.Split(".").Last());
                    methodInfo.OutputMessage = new MessageInfo(method.OutputType.Split(".").Last());
                    _descriptor.Methods.Add(methodInfo);
                }
            }

            return UpdateContractDescriptor();
        }

        public List<string> GetContractMethods()
        {
            return Descriptor.Methods.Select(o => o.MethodName).ToList();
        }

        public void GetAllMethodsInfo(bool withDetails = false)
        {
            var methods = GetContractMethods();
            var count = 0;
            foreach (var method in methods)
            {
                $"{count++: 00}. {method}".WriteSuccessLine();
                if (withDetails)
                    GetParameters(method);
            }

            Console.WriteLine();
        }

        public void GetParameters(string methodName)
        {
            var method = Descriptor.Methods.FirstOrDefault(o => o.MethodName == methodName);
            if (method == null) return;
            $"[Input]: {method.InputMessage.Name}".WriteWarningLine();
            var inputIndex = 1;
            foreach (var parameter in method.InputMessage.Fields)
                $"Index: {inputIndex++}  Name: {parameter.PadRight(24)}".WriteWarningLine();

            $"[Output]: {method.OutputMessage.Name}".WriteWarningLine();
            var outputIndex = 0;
            foreach (var parameter in method.OutputMessage.Fields)
                $"Index: {outputIndex++}  Name: {parameter.PadRight(24)}".WriteWarningLine();
        }

        private ContractDescriptor UpdateContractDescriptor()
        {
            var messageInfos = _descriptor.MessageInfos;
            foreach (var methodInfo in _descriptor.Methods)
            {
                methodInfo.InputMessage.Fields =
                    messageInfos.FirstOrDefault(o => o.Name == methodInfo.InputMessage.Name)?.Fields ??
                    new List<string>();
                methodInfo.OutputMessage.Fields =
                    messageInfos.FirstOrDefault(o => o.Name == methodInfo.OutputMessage.Name)?.Fields ??
                    new List<string>();
            }

            return _descriptor;
        }

        private static string ConvertNameToJsonName(string name)
        {
            if (!name.Contains("_")) return name;
            var array = name.Split("_");
            var jsonName = array[0];
            for (var i = 1; i < array.Length; i++)
                jsonName += array[i].Substring(0, 1).ToUpper() + array[i].Substring(1);

            return jsonName;
        }

        private bool IsAddressInfo(string info, out Address address)
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

        private bool IsHashInfo(string info, out Hash hash)
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
*/

