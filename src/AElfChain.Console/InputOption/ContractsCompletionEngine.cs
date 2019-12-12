using System;
using System.Collections.Generic;
using System.Linq;

namespace AElfChain.Console.InputOption
{
    public class ContractsCompletionEngine : ICompletionEngine
    {
        private readonly char[] _tokenDelimiters = {' '};

        public ContractsCompletionEngine(Dictionary<string, string> systemContracts)
        {
            SystemContracts = systemContracts;
        }

        public Dictionary<string, string> SystemContracts { get; set; }
        public ConsoleKeyInfo Trigger { get; } = new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false);

        public string[] GetAllSelections()
        {
            return SystemContracts.Keys.ToArray();
        }

        public string[] GetCompletions(string partial)
        {
            var keys = SystemContracts.Keys.Where(o => o.ToLower().StartsWith(partial)).ToList();
            var contracts = new List<string>();
            foreach (var key in keys) contracts.Add(SystemContracts[key]);

            return contracts.ToArray();
        }

        public char[] GetTokenDelimiters()
        {
            return _tokenDelimiters;
        }
    }
}