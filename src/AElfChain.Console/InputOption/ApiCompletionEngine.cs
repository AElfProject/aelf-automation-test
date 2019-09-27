using System;
using System.Collections.Generic;
using System.Linq;

namespace AElfChain.Console.InputOption
{
    public class ApiCompletionEngine : ICompletionEngine
    {
        public readonly char[] _tokenDelimiters = {' '};
        public ConsoleKeyInfo Trigger { get; } = new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false);
        public List<string> ApiCommands => new List<string>
        {
            "BlockHeight",
            "BlockByHash",
            "BlockByHeight",
            "TransactionPoolStatus",
            "BlockState",
            "CurrentRoundInformation",
            "ChainStatus",
            "TaskQueueStatus",
            "TransactionResult",
            "TransactionResults"
        };

        public ApiCompletionEngine()
        {
        }
        
        public string[] GetCompletions(string partial)
        {
            return ApiCommands.Where(o => o.ToLower().StartsWith(partial.ToLower())).ToArray();
        }

        public char[] GetTokenDelimiters() => _tokenDelimiters;
    }
}