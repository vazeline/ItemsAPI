using Common.Utility.Classes.DependencyInjection.Interfaces;
using Items.Data.EFCore.Abstraction;
using Items.Domain.DomainRepositories.Actions.Interfaces;
using Items.Domain.DomainRepositories.Interfaces;

namespace Items.Domain.DomainRepositories
{
    public class ItemsUnitOfWork : UnitOfWork, IItemsUnitOfWork
    {
        private readonly ILazyDependencyResolver lazyDependencyResolver;

        public ItemsUnitOfWork(ItemsContext context, ILazyDependencyResolver lazyDependencyResolver)
            : base(context)
        {
            this.lazyDependencyResolver = lazyDependencyResolver;
        }

        public IAuditLogRepository AuditLogRepository => this.lazyDependencyResolver.Get<IAuditLogRepository>();
        public IItemRepository ItemRepository => this.lazyDependencyResolver.Get<IItemRepository>();

        internal new ItemsContext Context => (ItemsContext)base.Context;
    }
}
