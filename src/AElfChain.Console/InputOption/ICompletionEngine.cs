using System;

namespace AElfChain.Console.InputOption
{
    public interface ICompletionEngine
    {
        ConsoleKeyInfo Trigger { get; }
        string[] GetAllSelections();
        string[] GetCompletions(string partial);
        char[] GetTokenDelimiters();
    }
}