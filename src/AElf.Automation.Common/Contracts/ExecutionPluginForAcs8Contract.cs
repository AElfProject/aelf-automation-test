using AElf.Automation.Common.Helpers;

namespace AElf.Automation.Common.Contracts
{
    public enum PluginAcs8Method 
    {
        CpuConsumingMethod,
        StoConsumingMethod,
        NetConsumingMethod,
        FewConsumingMethod
    }
    
    public class ExecutionPluginForAcs8Contract : BaseContract<PluginAcs8Method>
    {
        public ExecutionPluginForAcs8Contract(IApiHelper apiHelper, string callAddress, string contractAddress) 
            : base(apiHelper, callAddress)
        {
            CallAddress = callAddress;
            UnlockAccount(CallAddress);
        }
    }
}