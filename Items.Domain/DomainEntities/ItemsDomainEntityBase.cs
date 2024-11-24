using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Items.Data.EFCore.Entities;
using Items.Data.EFCore.Entities.Interfaces;
using Items.Domain.DomainRepositories.Interfaces;
using Common.Utility;

namespace Items.Domain.DomainEntities
{
    public abstract class ItemsDomainEntityBase : DomainEntityBase, IIdentity
    {
        protected ItemsDomainEntityBase()
        {
        }

        protected ItemsDomainEntityBase(int id)
        {
            this.Id = id;
        }

        public int Id { get; protected set; }

        internal class Services
        {
            protected Services()
            {
            }

            internal static IItemsUnitOfWork ItemsUnitOfWork => GetServiceFromCurrentServiceProvider<IItemsUnitOfWork>();
        }
    }

    public abstract class ItemsDomainEntityBase<TDomainEntity> : ItemsDomainEntityBase
        where TDomainEntity : class, IIdentity
    {
        internal void AddDomainEntityToReadOnlyDependentCollectionForInternalOperations<TDependent>(
            Expression<Func<TDomainEntity, IReadOnlyList<TDependent>>> readOnlyDependentCollectionGetter,
            TDependent itemToAdd)
            where TDependent : IIdentity
        {
            if (this is not TDomainEntity domainEntity)
            {
                throw new InvalidOperationException();
            }

            var items = readOnlyDependentCollectionGetter.Compile()(domainEntity).ToList();
            items.Add(itemToAdd);

            var readOnlyDependentCollectionSetter = ExpressionUtility.GetterToSetter(readOnlyDependentCollectionGetter);
            readOnlyDependentCollectionSetter(domainEntity, items);
        }

        internal void RemoveDomainEntityFromReadOnlyDependentCollectionForInternalOperations<TDependent>(
            Expression<Func<TDomainEntity, IReadOnlyList<TDependent>>> readOnlyDependentCollectionGetter,
            TDependent itemToRemove,
            IItemsUnitOfWork unitOfWork,
            bool alsoDeleteFromDatabase)
            where TDependent : IIdentity
        {
            if (this is not TDomainEntity domainEntity)
            {
                throw new InvalidOperationException();
            }

            var repository = unitOfWork.GetRepository<TDependent>() ?? throw new InvalidOperationException();

            // removing the item from the collection will sever the foreign key relationship, but not delete it
            var items = readOnlyDependentCollectionGetter.Compile()(domainEntity).ToList();
            items.Remove(itemToRemove);

            var readOnlyDependentCollectionSetter = ExpressionUtility.GetterToSetter(readOnlyDependentCollectionGetter);
            readOnlyDependentCollectionSetter(domainEntity, items);

            // removing from the repository actually deletes the item too
            if (alsoDeleteFromDatabase)
            {
                repository.Remove(itemToRemove);
            }
        }
    }
}
