using System;

namespace AElfChain.Console.InputOption
{
    public class EmptyCompletionEngine : ICompletionEngine
    {
        private readonly string[] _completions = new string[0];
        private readonly char[] _tokenDelimiters = new char[0];
        public ConsoleKeyInfo Trigger => default;

        public string[] GetAllSelections()
        {
            return _completions;
        }

        public string[] GetCompletions(string partial)
        {
            return _completions;
        }

        public char[] GetTokenDelimiters()
        {
            return _tokenDelimiters;
        }
    }
}