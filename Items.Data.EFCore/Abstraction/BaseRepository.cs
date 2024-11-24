using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Common.ExtensionMethods;
using Common.Models.Interfaces;
using Common.Utility;
using Items.Data.EFCore.Abstraction.Interfaces;
using Items.Data.EFCore.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Items.Data.EFCore.Abstraction
{
    public abstract class BaseRepository<TDomainEntity, TUnitOfWork> : IBaseRepository<TDomainEntity>, IBaseRepositoryInternal
        where TDomainEntity : class, IIdentity
        where TUnitOfWork : class, IUnitOfWork
    {
        protected BaseRepository(TUnitOfWork unitOfWork)
        {
            this.UnitOfWork = unitOfWork;
        }

        DbContext IBaseRepositoryInternal.Context => this.Context;

        protected virtual Func<IQueryable<TDomainEntity>, IIncludableQueryable<TDomainEntity, object>> DefaultIncludes { get; }

        protected TUnitOfWork UnitOfWork { get; }

        protected virtual Dictionary<string, Expression<Func<TDomainEntity, object>>> SortFieldKeyMap { get; } = new Dictionary<string, Expression<Func<TDomainEntity, object>>>();

        private DbContext Context => (this.UnitOfWork as UnitOfWork).GetContext();

        public void DisableTracking()
        {
            this.Context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        public void EnableTracking()
        {
            this.Context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.TrackAll;
        }

        public virtual List<TDomainEntity> List()
        {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits - required
            return Task.Run(async () => await this.ListByQueryAsync()).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        }

        public virtual async Task<List<TDomainEntity>> ListAsync()
        {
            return await this.ListByQueryAsync();
        }

        public virtual async Task<List<TDomainEntity>> ListByIdListAsync(IEnumerable<int> ids)
        {
            return await this.ListByQueryAsync(predicate: x => ids.Contains(x.Id) );
        }

        public virtual TDomainEntity GetById(int id)
        {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits - required
            return Task.Run(async () => await this.GetByIdAsync(id)).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        }

        public virtual async Task<TDomainEntity> GetByIdAsync(int id)
        {
            return await this.GetByQueryAsync(predicate: x => x.Id == id);
        }

        public virtual void Add(TDomainEntity entity)
        {
            this.Context.Set<TDomainEntity>().Add(entity);
        }

        public virtual async Task AddAsync(TDomainEntity entity)
        {
            await this.Context.Set<TDomainEntity>().AddAsync(entity);
        }

        public virtual Task UpdateAsync(TDomainEntity entity)
        {
            /* default implementation requires nothing, because EF tracks changes for us
             * however, if this class is being used in conjunction with an extra Domain abstraction layer
             * then there will be need to manually map the domain entity to the database entities it consists of
             * that logic can be encapsulated in here, in a class that extends from this one */

            // some repository methods may clear the change tracker due to 10,000s of entities being loaded
            // if we try to update these methods, the update will fail silently and give a confusing result
            // better to trap this scenario here, and throw an exception, enduring if we want to update an entity
            // that we have always intentionally loaded it from the context with tracking
            if (this.Context.Entry(entity).State == EntityState.Detached)
            {
                throw new InvalidOperationException("Attempt to update detached entity - you should work only with tracked entities from the context");
            }

            return Task.CompletedTask;
        }

        public virtual async Task DeleteByIdAsync(int id)
        {
            var set = this.Context.Set<TDomainEntity>();
            var entity = await set.FindAsync(id);

            if (entity == null)
            {
                throw new KeyNotFoundException($"Entity with Id {id} does not exist");
            }

            set.Remove(entity);
        }

        public virtual async Task TruncateAsync()
        {
            var set = this.Context.Set<TDomainEntity>();
            await set.ExecuteDeleteAsync();
        }

        public virtual void Remove(TDomainEntity entity)
        {
            this.Context.Set<TDomainEntity>().Remove(entity);
        }

        public virtual void RemoveRange(IEnumerable<TDomainEntity> entities)
        {
            this.Context.Set<TDomainEntity>().RemoveRange(entities);
        }

        public virtual async Task<bool> ExistsAsync(int id)
        {
            return await this.AnyAsync(x => x.Id == id);
        }

        public virtual async Task<bool> ExistsAsync(IEnumerable<int> ids)
        {
            ids.ThrowIfNull();

            return await this.CountAsync(x => ids.Contains(x.Id) ) == ids.Count();
        }

        public virtual async Task<TDomainEntity> SingleOrDefaultAsync(Expression<Func<TDomainEntity, bool>> predicate = null)
        {
            return await this.Context.Set<TDomainEntity>().SingleOrDefaultAsync(predicate, default);
        }

        public virtual async Task<TDomainEntity> FirstOrDefaultAsync(Expression<Func<TDomainEntity, bool>> predicate = null)
        {
            return await this.Context.Set<TDomainEntity>().FirstOrDefaultAsync(predicate, default);
        }

        public virtual bool Any(Expression<Func<TDomainEntity, bool>> predicate = null)
        {
            if (predicate == null)
            {
                return this.Context.Set<TDomainEntity>().Any();
            }

            return this.Context.Set<TDomainEntity>().Any(predicate);
        }

        public virtual async Task<bool> AnyAsync(Expression<Func<TDomainEntity, bool>> predicate = null)
        {
            if (predicate == null)
            {
                return await this.Context.Set<TDomainEntity>().AnyAsync();
            }

            return await this.Context.Set<TDomainEntity>().AnyAsync(predicate);
        }

        public virtual async Task<int> CountAsync(Expression<Func<TDomainEntity, bool>> predicate = null)
        {
            if (predicate == null)
            {
                return await this.Context.Set<TDomainEntity>().CountAsync();
            }

            return await this.Context.Set<TDomainEntity>().CountAsync(predicate);
        }

        public async Task<TDomainEntity> GetByQueryAsync(
            Expression<Func<TDomainEntity, bool>> predicate,
            Func<IQueryable<TDomainEntity>, IIncludableQueryable<TDomainEntity, object>> includes = null,
            Func<IQueryable<TDomainEntity>, IOrderedQueryable<TDomainEntity>> orderBy = null,
            Expression<Func<TDomainEntity, TDomainEntity>> projection = null,
            bool disableTracking = false,
            bool ignoreQueryFilters = false,
            bool useQuerySplitting = false)
        {
            var query = this.GetQuery(
                predicate: predicate,
                includes: includes,
                orderBy: orderBy,
                projection: projection,
                disableTracking: disableTracking,
                ignoreQueryFilters: ignoreQueryFilters,
                useQuerySplitting: useQuerySplitting);

            return await query.SingleOrDefaultAsync();
        }

        public async Task<List<TDomainEntity>> ListByQueryAsync(
            Expression<Func<TDomainEntity, bool>> predicate = null,
            Func<IQueryable<TDomainEntity>, IIncludableQueryable<TDomainEntity, object>> includes = null,
            Func<IQueryable<TDomainEntity>, IOrderedQueryable<TDomainEntity>> orderBy = null,
            IPagingRequest paging = null,
            Expression<Func<TDomainEntity, TDomainEntity>> projection = null,
            bool disableTracking = false,
            bool ignoreQueryFilters = false,
            bool useQuerySplitting = false)
        {
            var queryable = this.GetQuery(
                predicate: predicate,
                includes: includes,
                orderBy: orderBy,
                projection: projection,
                paging: paging,
                disableTracking: disableTracking,
                ignoreQueryFilters: ignoreQueryFilters,
                useQuerySplitting: useQuerySplitting);

            return await queryable.ToListAsync();
        }

        public async Task<List<TProjection>> GroupByQueryAsync<TGroupingKey, TProjection>(
            Expression<Func<TDomainEntity, TGroupingKey>> groupBy,
            Expression<Func<IGrouping<TGroupingKey, TDomainEntity>, TProjection>> projection,
            Expression<Func<TDomainEntity, bool>> predicate = null,
            Func<IQueryable<TDomainEntity>, IIncludableQueryable<TDomainEntity, object>> includes = null,
            Func<IQueryable<TDomainEntity>, IOrderedQueryable<TDomainEntity>> orderBy = null,
            bool disableTracking = false,
            bool ignoreQueryFilters = false,
            bool useQuerySplitting = false)
        {
            var queryable = this.GetGroupedQuery(
                groupBy: groupBy,
                projection: projection,
                predicate: predicate,
                includes: includes,
                orderBy: orderBy,
                disableTracking: disableTracking,
                ignoreQueryFilters: ignoreQueryFilters,
                useQuerySplitting: useQuerySplitting);

            return await queryable.ToListAsync();
        }

        /// <summary>
        /// Used in preference to Include(), when include would explode too much data from the db in one round-trip.
        /// </summary>
        public async Task LoadByQueryAsync(
            Expression<Func<TDomainEntity, bool>> predicate,
            Func<IQueryable<TDomainEntity>, IIncludableQueryable<TDomainEntity, object>> includes = null,
            Func<IQueryable<TDomainEntity>, IOrderedQueryable<TDomainEntity>> orderBy = null,
            IPagingRequest paging = null,
            Expression<Func<TDomainEntity, TDomainEntity>> projection = null,
            bool disableTracking = false,
            bool ignoreQueryFilters = false,
            bool useQuerySplitting = false)
        {
            var queryable = this.GetQuery(
                predicate: predicate,
                includes: includes,
                orderBy: orderBy,
                projection: projection,
                paging: paging,
                disableTracking: disableTracking,
                ignoreQueryFilters: ignoreQueryFilters,
                useQuerySplitting: useQuerySplitting);

            await queryable.LoadAsync();
        }

        public Func<IQueryable<TDomainEntity>, IOrderedQueryable<TDomainEntity>> SortingRequestToOrderBy(ISortingRequest sortingRequest)
        {
            if (sortingRequest?.Items?.Any() != true)
            {
                return null;
            }

            return queryable =>
            {
                var anyOrderingApplied = false;

                foreach (var sortItem in sortingRequest.Items)
                {
                    sortItem.FieldKey.ThrowIfNullOrWhiteSpace();

                    if (!this.SortFieldKeyMap.TryGetValue(sortItem.FieldKey.ToLower(), out var fieldExpression))
                    {
                        throw new ArgumentOutOfRangeException($"Field with key '{sortItem.FieldKey.ToLower()}' does not exist in field map");
                    }

                    if (!anyOrderingApplied)
                    {
                        if (!sortItem.IsDescending)
                        {
                            queryable = queryable.OrderBy(fieldExpression);
                        }
                        else
                        {
                            queryable = queryable.OrderByDescending(fieldExpression);
                        }
                    }
                    else if (!sortItem.IsDescending)
                    {
                        queryable = (queryable as IOrderedQueryable<TDomainEntity>).ThenBy(fieldExpression);
                    }
                    else
                    {
                        queryable = (queryable as IOrderedQueryable<TDomainEntity>).ThenByDescending(fieldExpression);
                    }

                    anyOrderingApplied = true;
                }

                return queryable as IOrderedQueryable<TDomainEntity>;
            };
        }

        private IQueryable<TDomainEntity> GetQuery(
            Expression<Func<TDomainEntity, bool>> predicate = null,
            Func<IQueryable<TDomainEntity>, IIncludableQueryable<TDomainEntity, object>> includes = null,
            Func<IQueryable<TDomainEntity>, IOrderedQueryable<TDomainEntity>> orderBy = null,
            IPagingRequest paging = null,
            Expression<Func<TDomainEntity, TDomainEntity>> projection = null,
            bool disableTracking = false,
            bool ignoreQueryFilters = false,
            bool useQuerySplitting = false)
        {
            var queryable = !disableTracking ? this.GetDbSet().AsQueryable() : this.GetDbSetAsNoTracking();

            if (ignoreQueryFilters)
            {
                queryable = queryable.IgnoreQueryFilters();
            }

            if (includes != null)
            {
                queryable = includes(queryable);
            }
            else if (this.DefaultIncludes != null)
            {
                queryable = this.DefaultIncludes(queryable);
            }

            if (predicate != null)
            {
                queryable = queryable.Where(predicate);
            }

            if (orderBy != null)
            {
                queryable = orderBy(queryable);
            }
            else
            {
                queryable = queryable.OrderBy(x => x.Id);
            }

            if (projection != null)
            {
                queryable = queryable.Select(projection);
            }

            if ((paging?.PageSize ?? default) != default)
            {
                if (paging.PageIndex != default)
                {
                    queryable = queryable.Skip(paging.PageSize * paging.PageIndex);
                }

                queryable = queryable.Take(paging.PageSize);
            }

            if (useQuerySplitting)
            {
                queryable = queryable.AsSplitQuery();
            }

            return queryable;
        }

        private IQueryable<TProjection> GetGroupedQuery<TGroupingKey, TProjection>(
            Expression<Func<TDomainEntity, TGroupingKey>> groupBy,
            Expression<Func<IGrouping<TGroupingKey, TDomainEntity>, TProjection>> projection,
            Expression<Func<TDomainEntity, bool>> predicate = null,
            Func<IQueryable<TDomainEntity>, IIncludableQueryable<TDomainEntity, object>> includes = null,
            Func<IQueryable<TDomainEntity>, IOrderedQueryable<TDomainEntity>> orderBy = null,
            bool disableTracking = false,
            bool ignoreQueryFilters = false,
            bool useQuerySplitting = false)
        {
            var queryable = !disableTracking ? this.GetDbSet().AsQueryable() : this.GetDbSetAsNoTracking();

            if (ignoreQueryFilters)
            {
                queryable = queryable.IgnoreQueryFilters();
            }

            if (includes != null)
            {
                queryable = includes(queryable);
            }
            else if (this.DefaultIncludes != null)
            {
                queryable = this.DefaultIncludes(queryable);
            }

            if(predicate != null)
            {
                queryable = queryable.Where(predicate);
            }

            if (orderBy != null)
            {
                queryable = orderBy(queryable);
            }
            else
            {
                queryable = queryable.OrderBy(x => x.Id);
            }

            if (useQuerySplitting)
            {
                queryable = queryable.AsSplitQuery();
            }

            var groupedQueryable = queryable.GroupBy(groupBy);
            var projectedQueryable = groupedQueryable.Select(projection);

            return projectedQueryable;
        }

        private DbSet<TDomainEntity> GetDbSet()
        {
            return this.Context.Set<TDomainEntity>();
        }

        private IQueryable<TDomainEntity> GetDbSetAsNoTracking()
        {
            return this.Context.Set<TDomainEntity>().AsNoTracking();
        }
    }
}
