using System.Linq;
using AElfChain.Common.Helpers;
using AElfChain.Console.InputOption;
using Sharprompt;

namespace AElfChain.Console.Commands
{
    public class CommandOption
    {
        public static bool TryParseParameters(string input, int length, out string[] parameters)
        {
            parameters = ParseArguments(input);
            var result = parameters.Length >= length;

            if (!result)
                $"Wrong input parameters, parameter needed {parameters.Length}/{length}.input again".WriteErrorLine();

            return result;
        }

        public static string[] InputParameters(int length, string promptMsg = "")
        {
            if (promptMsg == string.Empty)
                promptMsg = "Input parameter";
            while (true)
            {
                var input = Prompt.Input<string>(promptMsg, validators: new[] {Validators.Required()});
                var result = TryParseParameters(input, length, out var parameters);

                if (!result) continue;

                return parameters;
            }
        }

        public static string[] InputParameters(int length, ConsoleReader reader)
        {
            while (true)
            {
                "[Selection(Tab)]: ".WriteSuccessLine(changeLine: false);
                var input = reader.ReadLine();
                if (input == "list")
                {
                    string.Join("\r\n", reader.CompletionEngine.GetAllSelections()).WriteSuccessLine();
                    continue;
                }

                var result = TryParseParameters(input, length, out var parameters);

                if (!result) continue;

                return parameters;
            }
        }

        private static string[] ParseArguments(string input)
        {
            var paramChars = input.ToCharArray();
            var inQuote = false;
            for (var index = 0; index < paramChars.Length; index++)
            {
                if (paramChars[index] == '"')
                    inQuote = !inQuote;
                if (!inQuote && paramChars[index] == ' ')
                    paramChars[index] = '\n';
            }

            var array = new string(paramChars).Split('\n');
            for (var i = 0; i < array.Length; i++) array[i] = array[i].Replace("\"", "");
            return array.ToArray();
        }
    }
}