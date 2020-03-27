namespace AElfChain.Contract
{
    public class MethodInfo
    {
        public MethodInfo(string methodName)
        {
            MethodName = methodName;
        }

        public string MethodName { get; set; }
        public MessageInfo InputMessage { get; set; }
        public MessageInfo OutputMessage { get; set; }
    }
}