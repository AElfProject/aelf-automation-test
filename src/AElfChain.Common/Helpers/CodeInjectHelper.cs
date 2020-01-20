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
            var method = asm.MainModule.Types.Where(t => t.Methods.Count > 0).First().Methods.First();
            var ilProcessor = method.Body.GetILProcessor();
            ilProcessor.InsertAfter(method.Body.Instructions.First(), ilProcessor.Create(OpCodes.Nop));
            var newCode = new MemoryStream();
            asm.Write(newCode);

            return newCode.ToArray();
        }
    }
}