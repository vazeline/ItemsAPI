using System.Threading.Tasks;
using Common.Models;

namespace Items.Data.EFCore.Abstraction.Interfaces
{
    public interface IUnitOfWorkTransaction
    {
        void AddResult(OperationResult result);

        Task CommitAsync(OperationResult resultToAddErrorsToOnRollbackFailure = null);

        Task TryRollbackAsync(OperationResult resultToAddErrorsToOnRollbackFailure = null);
    }
}