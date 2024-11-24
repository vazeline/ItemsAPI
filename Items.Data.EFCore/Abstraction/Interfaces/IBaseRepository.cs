using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Common.Models;
using Common.Models.Interfaces;
using Items.Data.EFCore.Entities.Interfaces;
using Microsoft.EntityFrameworkCore.Query;

namespace Items.Data.EFCore.Abstraction.Interfaces
{
    public interface IBaseRepository<TDomainEntity>
        where TDomainEntity : IIdentity
    {
        void DisableTracking();

        void EnableTracking();

        List<TDomainEntity> List();

        Task<List<TDomainEntity>> ListAsync();

        Task<List<TDomainEntity>> ListByIdListAsync(IEnumerable<int> ids);

        TDomainEntity GetById(int id);

        Task<TDomainEntity> GetByIdAsync(int id);

        void Add(TDomainEntity entity);

        Task AddAsync(TDomainEntity entity);

        Task UpdateAsync(TDomainEntity entity);

        Task DeleteByIdAsync(int id);
        Task TruncateAsync();

        void Remove(TDomainEntity entity);

        void RemoveRange(IEnumerable<TDomainEntity> entities);

        Task<TDomainEntity> SingleOrDefaultAsync(Expression<Func<TDomainEntity, bool>> predicate = null);

        Task<TDomainEntity> FirstOrDefaultAsync(Expression<Func<TDomainEntity, bool>> predicate = null);

        bool Any(Expression<Func<TDomainEntity, bool>> predicate = null);

        Task<bool> AnyAsync(Expression<Func<TDomainEntity, bool>> predicate = null);

        Task<int> CountAsync(Expression<Func<TDomainEntity, bool>> predicate = null);

        Task<bool> ExistsAsync(int id);

        Task<bool> ExistsAsync(IEnumerable<int> ids);

        Task<TDomainEntity> GetByQueryAsync(
            Expression<Func<TDomainEntity, bool>> predicate,
            Func<IQueryable<TDomainEntity>, IIncludableQueryable<TDomainEntity, object>> includes = null,
            Func<IQueryable<TDomainEntity>, IOrderedQueryable<TDomainEntity>> orderBy = null,
            Expression<Func<TDomainEntity, TDomainEntity>> projection = null,
            bool disableTracking = false,
            bool ignoreQueryFilters = false,
            bool useQuerySplitting = false);

        Task<List<TDomainEntity>> ListByQueryAsync(
            Expression<Func<TDomainEntity, bool>> predicate = null,
            Func<IQueryable<TDomainEntity>, IIncludableQueryable<TDomainEntity, object>> includes = null,
            Func<IQueryable<TDomainEntity>, IOrderedQueryable<TDomainEntity>> orderBy = null,
            IPagingRequest paging = null,
            Expression<Func<TDomainEntity, TDomainEntity>> projection = null,
            bool disableTracking = false,
            bool ignoreQueryFilters = false,
            bool useQuerySplitting = false);

        Task LoadByQueryAsync(
            Expression<Func<TDomainEntity, bool>> predicate,
            Func<IQueryable<TDomainEntity>, IIncludableQueryable<TDomainEntity, object>> includes = null,
            Func<IQueryable<TDomainEntity>, IOrderedQueryable<TDomainEntity>> orderBy = null,
            IPagingRequest paging = null,
            Expression<Func<TDomainEntity, TDomainEntity>> projection = null,
            bool disableTracking = false,
            bool ignoreQueryFilters = false,
            bool useQuerySplitting = false);

        Task<List<TProjection>> GroupByQueryAsync<TGroupingKey, TProjection>(
            Expression<Func<TDomainEntity, TGroupingKey>> groupBy,
            Expression<Func<IGrouping<TGroupingKey, TDomainEntity>, TProjection>> projection,
            Expression<Func<TDomainEntity, bool>> predicate = null,
            Func<IQueryable<TDomainEntity>, IIncludableQueryable<TDomainEntity, object>> includes = null,
            Func<IQueryable<TDomainEntity>, IOrderedQueryable<TDomainEntity>> orderBy = null,
            bool disableTracking = false,
            bool ignoreQueryFilters = false,
            bool useQuerySplitting = false);
    }
}
