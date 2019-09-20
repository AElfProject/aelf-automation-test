namespace AElfChain.Console.Commands
{
    public class MainSetConnectorCommand : BaseCommand
    {
        public MainSetConnectorCommand()
        {
            Name = "set-connector";
            Description = "Set new token connector";
            
            HelpOption("-h | -H | -? | --help");
            OnExecute(RunCommand);
        }

        protected override void InitOptions()
        {
            base.InitOptions();
            
        }

        protected override int RunCommand()
        {
            throw new System.NotImplementedException();
        }
    }
}