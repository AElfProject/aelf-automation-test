using Microsoft.Extensions.CommandLineUtils;

namespace AElfChain.Console.CommandOptions
{
    public static class CommandApplicationAndOptionExtensions
    {
        public static CommandOption AddOptionEndpoint(this CommandLineApplication commandLineApplication, bool required = true)
        {
            return commandLineApplication.Option("-e | --endpoint", Optional(required) + "Web api service endpoint url",
                CommandOptionType.SingleValue);
        }

        public static CommandOption AddOptionIsMainChain(this CommandLineApplication commandLineApplication, bool required = true)
        {
            return commandLineApplication.Option("-sc | --sidechain", Optional(required) + "Is side chain or not",
                CommandOptionType.SingleValue);
        }
        
        public static CommandOption AddOptionFromAddress(this CommandLineApplication commandLineApplication, bool required = true)
        {
            return commandLineApplication.Option("-f | --from", Optional(required) + "Transaction sender address",
                CommandOptionType.SingleValue);
        }
        
        public static CommandOption AddOptionToAddress(this CommandLineApplication commandLineApplication, bool required = true)
        {
            return commandLineApplication.Option("-t | --to", Optional(required) + "Transaction to address",
                CommandOptionType.SingleValue);
        }
        
        public static CommandOption AddOptionContractAddress(this CommandLineApplication commandLineApplication, bool required = true)
        {
            return commandLineApplication.Option("-c | --contract", Optional(required) + "Transaction contract address",
                CommandOptionType.SingleValue);
        }
        
        public static CommandOption AddOptionTokenSymbol(this CommandLineApplication commandLineApplication, bool required = true)
        {
            return commandLineApplication.Option("-s | --symbol", Optional(required) + "Token symbol",
                CommandOptionType.SingleValue);
        }
        
        public static CommandOption AddOptionAmount(this CommandLineApplication commandLineApplication, bool required = true)
        {
            return commandLineApplication.Option("-a | --amount", Optional(required) + "Amount info",
                CommandOptionType.SingleValue);
        }

        public static string Optional(bool required)
        {
            return !required ? "Optional" : "";
        }
    }
}