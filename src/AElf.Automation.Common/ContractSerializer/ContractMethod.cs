using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Automation.Common.Helpers;
using Google.Protobuf.Reflection;

namespace AElf.Automation.Common.ContractSerializer
{
    public class ContractMethod : IComparable
    {
        public MethodDescriptor Descriptor { get; set; }
        public string Name { get; set; }
        public string Input => InputType.Name;
        public MessageDescriptor InputType { get; set; }
        public List<FieldDescriptor> InputFields { get; set; }
        public string Output => OutputType.Name; 
        public MessageDescriptor OutputType { get; set; }
        public List<FieldDescriptor> OutputFields { get; set; }

        public ContractMethod(MethodDescriptor method)
        {
            Descriptor = method;
            Name = method.Name;
            InputType = method.InputType;
            OutputType = method.OutputType;
            InputFields = InputType.Fields.InFieldNumberOrder().ToList();
            OutputFields = OutputType.Fields.InFieldNumberOrder().ToList();
        }

        public void GetMethodDescriptionInfo()
        {
            $"[Method]: {Name}".WriteWarningLine();
        }

        public void GetInputParameters()
        {
            $"[Input]: {Input}".WriteWarningLine();
            foreach (var parameter in InputFields)
            {
                if (parameter.Name == "value") continue;
                if(parameter.FieldType == FieldType.Message)
                    $"Index: {parameter.Index}  Name: {parameter.Name.PadRight(24)} Field: {parameter.MessageType.Name}".WriteWarningLine();
                else
                    $"Index: {parameter.Index}  Name: {parameter.Name.PadRight(24)} Field: {parameter.FieldType}".WriteWarningLine();    
            }
        }

        public void GetOutputParameters()
        {
            $"[Output]: {Output}".WriteWarningLine();
            foreach (var parameter in OutputFields)
            {
                if (parameter.Name == "value") continue;
                if(parameter.FieldType == FieldType.Message)
                    $"Index: {parameter.Index}  Name: {parameter.Name.PadRight(24)} Field: {parameter.MessageType.Name}".WriteWarningLine(); 
                else
                    $"Index: {parameter.Index}  Name: {parameter.Name.PadRight(24)} Field: {parameter.FieldType}".WriteWarningLine();    
            }
        }

        public int CompareTo(object obj)
        {
            ContractMethod info = obj as ContractMethod;
            return string.CompareOrdinal(this.Name, info.Name) > 0 ? 0 : 1;
        }
    }
}