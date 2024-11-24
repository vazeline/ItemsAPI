using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Common.Utility;
using Items.Data.EFCore.Abstraction.Interfaces;
using Items.Data.EFCore.ContextBaseClasses;
using Items.Data.EFCore.Entities;
using Items.Data.EFCore.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Items.Data.EFCore.Abstraction
{
    public abstract class UnitOfWork : IUnitOfWork
    {
        private DbContext context;
        private IUnitOfWorkTransaction transaction;

        public UnitOfWork(DbContext context)
        {
            this.context = context;

            this.IsDatabaseSqlServer = context.Database.IsSqlServer();
            this.IsDatabaseSqlite = context.Database.IsSqlite();
            this.IsDatabaseInMemory = context.Database.IsInMemory();
        }


        public bool IsDatabaseSqlServer { get; init; }

        public bool IsDatabaseSqlite { get; init; }

        public bool IsDatabaseInMemory { get; init; }

        public IUnitOfWorkTransaction Transaction => this.transaction;

        protected static Dictionary<Type, PropertyInfo> RepositoryPropertiesByDomainEntityTypeDict { get; set; }

        protected DbContext Context => this.context;

        public IBaseRepository<TDomainEntity> GetRepository<TDomainEntity>()
            where TDomainEntity : IIdentity
        {
            RepositoryPropertiesByDomainEntityTypeDict ??= GetRepositoryPropertiesByDomainEntityTypeDict(this.GetType());

            if (RepositoryPropertiesByDomainEntityTypeDict.TryGetValue(typeof(TDomainEntity), out var propertyInfo))
            {
                return (IBaseRepository<TDomainEntity>)propertyInfo.GetValue(this);
            }

            throw new InvalidOperationException($"Could not find a repository for domain entity type {typeof(TDomainEntity).Name}");
        }

        public async Task SaveChangesAsync()
        {
            await this.Context.SaveChangesAsync();
        }

        public async Task<IUnitOfWorkTransaction> BeginTransactionAsync()
        {
            if (this.transaction != null)
            {
                throw new InvalidOperationException("A transaction has already been started");
            }

            this.transaction = new UnitOfWorkTransaction(this);
            await (this.transaction as UnitOfWorkTransaction).BeginTransactionAsync();

            return this.transaction;
        }

        public bool IsRelationshipLoaded<TEntity, TRelationship>(
            TEntity entity,
            Expression<Func<TEntity, TRelationship>> navigationPropertyAccessor)
            where TEntity : DomainEntityBase
            where TRelationship : DomainEntityBase
        {
            var entry = ValidateEntityEntryIsNotDetached(this.context, entity, errorMessage: $"Cannot check if relationships on {typeof(TEntity).Name} are loaded because the given entity is detached from the change tracker");
            return entry.Reference(navigationPropertyAccessor).IsLoaded;
        }

        public bool IsRelationshipLoaded<TEntity, TRelationship>(
            TEntity entity,
            Expression<Func<TEntity, IEnumerable<TRelationship>>> navigationPropertyAccessor)
            where TEntity : DomainEntityBase
            where TRelationship : DomainEntityBase
        {
            var entry = ValidateEntityEntryIsNotDetached(this.context, entity, errorMessage: $"Cannot check if relationships on {typeof(TEntity).Name} are loaded because the given entity is detached from the change tracker");
            return entry.Collection(navigationPropertyAccessor).IsLoaded;
        }

        internal DbContext GetContext() => this.Context;

        internal void ResetTransaction() => this.transaction = null;

        protected static Dictionary<Type, PropertyInfo> GetRepositoryPropertiesByDomainEntityTypeDict(Type unitOfWorkType)
        {
            return unitOfWorkType.GetProperties()
                .Concat(unitOfWorkType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic))
                .Select(x => (x, TypeUtility.GetGenericInterfaceImplementations(x.PropertyType, typeof(IBaseRepository<>))))
                .Where(x => x.Item2.Count == 1)
                .ToDictionary(
                    x => x.Item2.Single().GetGenericArguments()[0],
                    x => x.x);
        }

        protected void SetContext(DbContext context)
        {
            this.context = context;
        }

        private static EntityEntry<TEntity> ValidateEntityEntryIsNotDetached<TEntity>(DbContext context, TEntity entity, string errorMessage = null)
            where TEntity : DomainEntityBase
        {
            var entry = context.Entry(entity);

            if (entry.State == EntityState.Detached)
            {
                throw new InvalidOperationException(errorMessage ?? $"Entity of type {typeof(TEntity).Name} is unexpectedly detached");
            }

            return entry;
        }
    }
}
