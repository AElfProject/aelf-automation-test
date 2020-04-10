using System;
using System.Collections.Generic;
using System.Linq;

namespace AElfChain.Console.InputOption
{
    public class CommandsCompletionEngine : ICompletionEngine
    {
        private readonly List<string> _commands;
        private readonly char[] _tokenDelimiters = {' '};

        public CommandsCompletionEngine(List<string> commands)
        {
            _commands = commands;
        }

        public ConsoleKeyInfo Trigger { get; } = new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false);

        public string[] GetAllSelections()
        {
            return _commands.ToArray();
        }

        public string[] GetCompletions(string partial)
        {
            return _commands.Where(cmd => cmd.ToLower().StartsWith(partial.ToLower())).ToArray();
        }

        public char[] GetTokenDelimiters()
        {
            return _tokenDelimiters;
        }
    }
}