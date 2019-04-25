using System.Threading.Tasks;

namespace AElf.Automation.TransactionExecution
{
    public interface IExcutor<T>
    {
        Task<T> ExecuteTransaction();
    }
}