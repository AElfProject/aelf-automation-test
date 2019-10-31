using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.Automation.Common.Helpers;
using ProtoBuf;
using FileDescriptorSet = Google.Protobuf.Reflection.FileDescriptorSet;

namespace AElf.Automation.Common.ContractSerializer
{
    public class MessageInfo
    {
        public string Name { get; set; }
        public List<string> Fields { get; set; }

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
    }

    public class MethodInfo
    {
        public string MethodName { get; set; }
        public MessageInfo InputMessage { get; set; }
        public MessageInfo OutputMessage { get; set; }

        public MethodInfo(string methodName)
        {
            MethodName = methodName;
        }
    }

    public class ContractDescriptor
    {
        public List<MessageInfo> MessageInfos { get; set; }
        public List<MethodInfo> Methods { get; set; }

        public ContractDescriptor()
        {
            MessageInfos = new List<MessageInfo>();
            Methods = new List<MethodInfo>();
        }
    }

    public class CustomContractHandler
    {
        private readonly byte[] _fileDescriptorBytes;

        public ContractDescriptor Descriptor => AnalyzeContractDescriptor();

        public CustomContractHandler(byte[] fileDescriptorBytes)
        {
            _fileDescriptorBytes = fileDescriptorBytes;
        }

        private ContractDescriptor _descriptor;

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
                    var messageInfo = new MessageInfo(message.Name, message.Fields.Select(o => o.Name).ToList());
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

        public void GetAllMethodsInfo()
        {
            var methods = GetContractMethods();
            foreach (var method in methods)
            {
                $"ServiceMethod: {method}".WriteSuccessLine();
                GetParameters(method);
                Console.WriteLine();
            }
        }

        public void GetParameters(string methodName)
        {
            var method = Descriptor.Methods.FirstOrDefault(o => o.MethodName == methodName);
            if (method == null) return;
            $"[Input]: {method.InputMessage.Name}".WriteWarningLine();
            var inputIndex = 1;
            foreach (var parameter in method.InputMessage.Fields)
            {
                $"Index: {inputIndex++}  Name: {parameter.PadRight(24)}".WriteWarningLine();
            }

            $"[Output]: {method.OutputMessage.Name}".WriteWarningLine();
            var outputIndex = 0;
            foreach (var parameter in method.OutputMessage.Fields)
            {
                $"Index: {outputIndex++}  Name: {parameter.PadRight(24)}".WriteWarningLine();
            }
        }

        private ContractDescriptor UpdateContractDescriptor()
        {
            var messageInfos = _descriptor.MessageInfos;
            foreach (var methodInfo in _descriptor.Methods)
            {
                methodInfo.InputMessage.Fields =
                    messageInfos.FirstOrDefault(o => o.Name == methodInfo.InputMessage.Name)?.Fields ?? new List<string>();
                methodInfo.OutputMessage.Fields =
                    messageInfos.FirstOrDefault(o => o.Name == methodInfo.OutputMessage.Name)?.Fields ?? new List<string>();
            }

            return _descriptor;
        }

        private readonly List<string> _ignoreProtoFiles = new List<string>
        {
            "google/protobuf/descriptor.proto",
            "google/protobuf/empty.proto",
            "google/protobuf/wrappers.proto"
        };
    }
}