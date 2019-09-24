using System;
using AElf.Automation.Common.Helpers;

namespace AElfChain.Console.Commands
{
    public class CommandOption
    {
        public static bool TryParseParameters(string input, int length, out string[] parameters)
        {
            parameters = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var result = parameters.Length >= length;
            
            if(!result)
                $"Wrong input parameters, parameter needed {parameters.Length}/{length}.input again".WriteErrorLine();

            return result;
        }

        public static string[] InputParameters(int length)
        {
            while (true)
            {
                "[Input parameter]: ".WriteSuccessLine(changeLine:false);
                var input = System.Console.ReadLine();
                var result = TryParseParameters(input, length, out var parameters);

                if(!result) continue;
                
                return parameters;
            }
        }
    }
}