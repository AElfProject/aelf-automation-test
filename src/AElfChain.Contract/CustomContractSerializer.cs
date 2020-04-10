using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Google.Protobuf.Reflection;
using ProtoBuf;

namespace AElfChain.Contract
{
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
            var descriptorSet = Serializer.Deserialize<FileDescriptorSet>(ms);

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
                Console.WriteLine($"{count++: 00}. {method}");
                if (withDetails)
                    GetParameters(method);
            }

            Console.WriteLine();
        }

        public void GetParameters(string methodName)
        {
            var method = Descriptor.Methods.FirstOrDefault(o => o.MethodName == methodName);
            if (method == null) return;
            Console.WriteLine($"[Input]: {method.InputMessage.Name}");
            var inputIndex = 1;
            foreach (var parameter in method.InputMessage.Fields)
                Console.WriteLine($"Index: {inputIndex++}  Name: {parameter.PadRight(24)}");

            Console.WriteLine($"[Output]: {method.OutputMessage.Name}");
            var outputIndex = 0;
            foreach (var parameter in method.OutputMessage.Fields)
                Console.WriteLine($"Index: {outputIndex++}  Name: {parameter.PadRight(24)}");
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
    }
}