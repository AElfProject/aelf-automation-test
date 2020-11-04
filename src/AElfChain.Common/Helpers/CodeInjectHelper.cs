using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AElfChain.Common.Helpers
{
    public class CodeInjectHelper
    {
        public static byte[] ChangeContractCodeHash(byte[] code)
        {
            var asm = AssemblyDefinition.ReadAssembly(new MemoryStream(code));
            var methodFirst = asm.MainModule.Types.Where(t => t.Methods.Count > 0).First().Methods.First();
            var methodLast = asm.MainModule.Types.Where(t => t.Methods.Count > 0).First().Methods.Last();
            var ilProcessorFirst = methodFirst.Body.GetILProcessor();
            var ilProcessorLast = methodLast.Body.GetILProcessor();
            var condition = DateTime.UtcNow.Second % 4;
            Console.WriteLine(condition);
            switch (condition)
            {
                case 0:
                    ilProcessorFirst.InsertAfter(methodFirst.Body.Instructions.First(), ilProcessorFirst.Create(OpCodes.Nop));
                    break;
                case 1:
                    ilProcessorLast.InsertBefore(methodLast.Body.Instructions.Last(), ilProcessorLast.Create(OpCodes.Nop));
                    break;
                case 2:
                    ilProcessorFirst.InsertBefore(methodFirst.Body.Instructions.Last(), ilProcessorFirst.Create(OpCodes.Nop));
                    break;
                case 3:
                    ilProcessorLast.InsertAfter(methodLast.Body.Instructions.First(), ilProcessorLast.Create(OpCodes.Nop));
                    break;
            }

            var newCode = new MemoryStream();
            asm.Write(newCode);

            return newCode.ToArray();
        }
    }
}