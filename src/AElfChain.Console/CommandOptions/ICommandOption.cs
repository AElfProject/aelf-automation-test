using Microsoft.Extensions.CommandLineUtils;

namespace AElfChain.Console.CommandOptions
{
    public interface ICommandOption
    {
        bool HasInputErrors { get; }

        void AddOptionToCommandLineApplication(CommandLineApplication commandLineApplication);
        void ParseAndValidateInput();
    }
}