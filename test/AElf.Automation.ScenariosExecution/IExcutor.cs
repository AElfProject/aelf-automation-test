using System.Threading.Tasks;

namespace AElf.Automation.ScenariosExecution
{
    public interface IExcutor<T>
    {
        Task<T> ExecuteTransaction();
    }
}