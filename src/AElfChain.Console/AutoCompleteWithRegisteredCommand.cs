using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;

namespace AElfChain.Console
{
    public class AutoCompleteWithRegisteredCommand : IAutoCompleteHandler
    {
        private Dictionary<string, string> _commands;

        public AutoCompleteWithRegisteredCommand(Dictionary<string, string> commandNames)
        {
            _commands = commandNames;
        }
        
        public string[] GetSuggestions(string text, int index)
        {
            var keys = _commands.Keys.Where(c => c.StartsWith(text)).ToList();
            var contracts = new List<string>();
            foreach (var key in keys)
            {
                contracts.Add(_commands[key]);
            }

            return contracts.ToArray();
        }

        public char[] Separators { get; set; } = {' '};
    }
}