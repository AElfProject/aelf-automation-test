using System.Collections.Generic;
using System.Linq;
using AElfChain.Common.Helpers;
using Google.Protobuf.Reflection;

namespace AElfChain.Common.ContractSerializer
{
    public class ContractInfo
    {
        private List<ServiceDescriptor> Descriptors { get; set; }

        public List<ContractMethod> Methods { get; set; }
        public List<string> ActionMethodNames { get; set; }

        public List<string> ViewMethodNames { get; set; }

        public ContractInfo(List<ServiceDescriptor> serviceDescriptors)
        {
            Descriptors = serviceDescriptors;
            Methods = new List<ContractMethod>();
            ActionMethodNames = new List<string>();
            ViewMethodNames = new List<string>();

            GetContractMethods();
        }

        public ContractMethod GetContractMethod(string name)
        {
            return Methods.First(o => o.Name == name);
        }

        public void GetContractMethodsInfo()
        {
            "Method List:".WriteSuccessLine();
            for (var i = 0; i < ActionMethodNames.Count; i++)
            {
                $"{ActionMethodNames[i].PadRight(40)}".WriteSuccessLine(changeLine: false);
                if (i % 4 == 3)
                    System.Console.WriteLine();
            }

            if (ActionMethodNames.Count % 4 != 0)
                System.Console.WriteLine();
        }

        public void GetContractViewMethodsInfo()
        {
            "Method List:".WriteSuccessLine();
            for (var i = 0; i < ViewMethodNames.Count; i++)
            {
                $"{ViewMethodNames[i].PadRight(40)}".WriteSuccessLine(changeLine: false);
                if (i % 4 == 3)
                    System.Console.WriteLine();
            }

            if (ViewMethodNames.Count % 4 != 0)
                System.Console.WriteLine();
        }

        private void GetContractMethods()
        {
            foreach (var descriptor in Descriptors)
            {
                foreach (var method in descriptor.Methods)
                {
                    Methods.Add(new ContractMethod(method));
                    ActionMethodNames.Add(method.Name);
                    if (method.OutputType.Name != "Empty")
                        ViewMethodNames.Add(method.Name);
                } 
            }
            
            //sort
            Methods.Sort();
            ActionMethodNames.Sort();
            ViewMethodNames.Sort();
        }
    }
}