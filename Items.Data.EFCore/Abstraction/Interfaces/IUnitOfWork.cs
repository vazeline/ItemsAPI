using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Items.Data.EFCore.Entities;
using Items.Data.EFCore.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Items.Data.EFCore.Abstraction.Interfaces
{
    public interface IUnitOfWork
    {
        IUnitOfWorkTransaction Transaction { get; }

        bool IsDatabaseSqlServer { get; init; }

        bool IsDatabaseSqlite { get; init; }

        bool IsDatabaseInMemory { get; init; }

        Task SaveChangesAsync();

        IBaseRepository<TDomainEntity> GetRepository<TDomainEntity>()
            where TDomainEntity : IIdentity;

        Task<IUnitOfWorkTransaction> BeginTransactionAsync();

        bool IsRelationshipLoaded<TEntity, TRelationship>(
            TEntity entity,
            Expression<Func<TEntity, TRelationship>> navigationPropertyAccessor)
            where TEntity : DomainEntityBase
            where TRelationship : DomainEntityBase;

        bool IsRelationshipLoaded<TEntity, TRelationship>(
            TEntity entity,
            Expression<Func<TEntity, IEnumerable<TRelationship>>> navigationPropertyAccessor)
            where TEntity : DomainEntityBase
            where TRelationship : DomainEntityBase;
    }
}
