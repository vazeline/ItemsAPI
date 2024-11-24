using Items.Data.EFCore.Abstraction.Interfaces;
using Items.Domain.DomainRepositories.Actions.Interfaces;

namespace Items.Domain.DomainRepositories.Interfaces
{
    public interface IItemsUnitOfWork : IUnitOfWork
    {
        IAuditLogRepository AuditLogRepository { get; }
        IItemRepository ItemRepository { get; }
    }
}
