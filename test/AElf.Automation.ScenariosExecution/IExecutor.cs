using System.Threading.Tasks;
using Google.Protobuf;

namespace AElf.Automation.ScenariosExecution
{
    public interface IExecutor<T>
    {
        Task<string> ExecuteTransaction(string method, IMessage input);

        Task<TResult> CallReadOnlyTransaction<TResult>(string method, IMessage input);
    }
}