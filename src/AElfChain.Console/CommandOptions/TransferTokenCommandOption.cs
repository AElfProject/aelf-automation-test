using Microsoft.Extensions.CommandLineUtils;

namespace AElfChain.Console.CommandOptions
{
    public class TransferTokenCommandOption : EndpointCommandOption
    {
        public CommandOption FromAddressOption { get; set; }
        public CommandOption ToAddressOption { get; set; }
        public CommandOption SymbolOption { get; set; }
        public CommandOption AmountOption { get; set; }
        
        public string From { get; set; }
        public string To { get; set; }
        public string Symbol { get; set; }
        public long Amount { get; set; }

        public override void AddOptionToCommandLineApplication(CommandLineApplication commandLineApplication)
        {
            base.AddOptionToCommandLineApplication(commandLineApplication);
            FromAddressOption = commandLineApplication.AddOptionFromAddress();
            ToAddressOption = commandLineApplication.AddOptionToAddress();
            SymbolOption = commandLineApplication.AddOptionTokenSymbol();
            AmountOption = commandLineApplication.AddOptionAmount();
        }

        public override void ParseAndValidateInput()
        {
            base.ParseAndValidateInput();
            From = FromAddressOption.TryParseAndValidateAddress(HasInputErrors);
            To = ToAddressOption.TryParseAndValidateAddress(HasInputErrors);
            Symbol = SymbolOption.TryParseRequiredString(HasInputErrors);
            Amount = AmountOption.TryParseAndValidateLong(HasInputErrors);
        }
    }
}