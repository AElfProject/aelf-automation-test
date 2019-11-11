using System;
using System.Collections.Generic;
using System.Linq;

namespace AElfChain.Console.InputOption
{
    public class ApiCompletionEngine : ICompletionEngine
    {
        public readonly char[] _tokenDelimiters = {' '};

        public List<string> ApiCommands => new List<string>
        {
            "BlockHeight",
            "BlockByHash",
            "BlockByHeight",
            "BlockState",
            "ChainStatus",
            "CurrentRoundInformation",
            "TaskQueueStatus",
            "TransactionResult",
            "TransactionResults",
            "TransactionPoolStatus",
            "ListAccounts"
        };

        public ConsoleKeyInfo Trigger { get; } = new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false);

        public string[] GetAllSelections()
        {
            return ApiCommands.ToArray();
        }

        public string[] GetCompletions(string partial)
        {
            return ApiCommands.Where(o => o.ToLower().StartsWith(partial.ToLower())).ToArray();
        }

        public char[] GetTokenDelimiters()
        {
            return _tokenDelimiters;
        }
    }
}