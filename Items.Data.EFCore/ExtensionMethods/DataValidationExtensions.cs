using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Common.ExtensionMethods;
using Common.Models;
using Items.Data.EFCore.Abstraction.Interfaces;
using Items.Data.EFCore.Entities;
using Items.Data.EFCore.Entities.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Items.Data.EFCore.ExtensionMethods
{
    public static class DataValidationExtensions
    {
        #region OperationResult extensions

        /// <summary>
        /// Overload for validation func <see cref="DependentRelationshipIsIncluded2{TDomainEntity, TRelationship}(OperationResult, TDomainEntity, IUnitOfWork, Expression{Func{TDomainEntity, TRelationship}}, string, string)"/> for a one-to-one relationship.
        /// </summary>
        public static OperationResult Validate<TDomainEntity, TRelationship>(
            this OperationResult result,
            TDomainEntity param,
            Func<OperationResult, TDomainEntity, IUnitOfWork, Expression<Func<TDomainEntity, TRelationship>>, string, string, bool> validationFunc,
            IUnitOfWork unitOfWork,
            Expression<Func<TDomainEntity, TRelationship>> navigationPropertyAccessor,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(navigationPropertyAccessor))] string navigationPropertyAccessorName = null)
            where TDomainEntity : DomainEntityBase
            where TRelationship : DomainEntityBase
        {
            return ValidationExtensions.ValidateInternal(
                result,
                () => validationFunc(result, param, unitOfWork, navigationPropertyAccessor, customErrorMessage, navigationPropertyAccessorName),
                haltOnFailure);
        }

        /// <summary>
        /// Overload for validation func <see cref="DependentRelationshipIsIncluded2{TDomainEntity, TRelationship}(OperationResult, TDomainEntity, IUnitOfWork, Expression{Func{TDomainEntity, TRelationship}}, string, string)"/> for a one-to-many relationship.
        /// </summary>
        public static OperationResult Validate<TDomainEntity, TRelationship>(
            this OperationResult result,
            TDomainEntity param,
            Func<OperationResult, TDomainEntity, IUnitOfWork, Expression<Func<TDomainEntity, IEnumerable<TRelationship>>>, string, string, bool> validationFunc,
            IUnitOfWork unitOfWork,
            Expression<Func<TDomainEntity, IEnumerable<TRelationship>>> navigationPropertyAccessor,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(navigationPropertyAccessor))] string navigationPropertyAccessorName = null)
            where TDomainEntity : DomainEntityBase
            where TRelationship : DomainEntityBase
        {
            return ValidationExtensions.ValidateInternal(
                result,
                () => validationFunc(result, param, unitOfWork, navigationPropertyAccessor, customErrorMessage, navigationPropertyAccessorName),
                haltOnFailure);
        }

        /// <summary>
        /// Overload for validation func to check dependent relationship exists for all parent items.
        /// </summary>
        public static OperationResult Validate<TParam>(
            this OperationResult result,
            IEnumerable<TParam> param,
            Func<OperationResult, IEnumerable<TParam>, Func<TParam, object>, string, string, string, bool> validationFunc,
            Func<TParam, object> subDependentSelector,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(param))] string paramName = null,
            [CallerArgumentExpression(nameof(subDependentSelector))] string subDependentSelectorName = null)
            where TParam : class, IIdentity
        {
            return ValidationExtensions.ValidateInternal(
                result,
                () => validationFunc(result, param, subDependentSelector, customErrorMessage, paramName, subDependentSelectorName),
                haltOnFailure);
        }

        /// <summary>
        /// Overload for validation func to check dependent collection item existence and return data.
        /// </summary>
        public static OperationResult Validate<TDomainEntity>(
            this OperationResult result,
            IEnumerable<TDomainEntity> collection,
            Func<OperationResult, IEnumerable<TDomainEntity>, int, Action<TDomainEntity>, string, string, bool> validationFunc,
            int domainEntityId,
            out TDomainEntity domainEntity,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(collection))] string paramName = null)
            where TDomainEntity : class, IIdentity
        {
            TDomainEntity tempDomainEntity = null;

            var validationResult = ValidationExtensions.ValidateInternal(
                result,
                () => validationFunc(result, collection, domainEntityId, x => tempDomainEntity = x, customErrorMessage, paramName),
                haltOnFailure);

            domainEntity = tempDomainEntity;

            return validationResult;
        }

        /// <summary>
        /// Overload for validation func to check repository item existence without retrieving data.
        /// </summary>
        public static OperationResult Validate<TDomainEntity>(
            this OperationResult result,
            int? domainEntityId,
            Func<OperationResult, int?, IBaseRepository<TDomainEntity>, string, string, bool> validationFunc,
            IBaseRepository<TDomainEntity> repository,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(domainEntityId))] string paramName = null)
            where TDomainEntity : class, IIdentity
        {
            return ValidationExtensions.ValidateInternal(
                result,
                () => validationFunc(result, domainEntityId, repository, customErrorMessage, paramName),
                haltOnFailure);
        }

        /// <summary>
        /// Overload for async validation func to check repository item existence without retrieving data.
        /// </summary>
        public static async Task<OperationResult> ValidateAsync<TDomainEntity>(
            this OperationResult result,
            int? domainEntityId,
            Func<OperationResult, int?, IBaseRepository<TDomainEntity>, string, string, Task<bool>> validationFunc,
            IBaseRepository<TDomainEntity> repository,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(domainEntityId))] string paramName = null)
            where TDomainEntity : class, IIdentity
        {
            return await ValidationExtensions.ValidateInternalAsync(
                result,
                () => validationFunc(result, domainEntityId, repository, customErrorMessage, paramName),
                haltOnFailure);
        }

        /// <summary>
        /// Overload for validation func to check multiple repository item existence without retrieving data.
        /// </summary>
        public static OperationResult Validate<TDomainEntity>(
            this OperationResult result,
            IEnumerable<int> domainEntityIds,
            Func<OperationResult, IEnumerable<int>, IBaseRepository<TDomainEntity>, string, string, bool> validationFunc,
            IBaseRepository<TDomainEntity> repository,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(domainEntityIds))] string paramName = null)
            where TDomainEntity : class, IIdentity
        {
            return ValidationExtensions.ValidateInternal(
                result,
                () => validationFunc(result, domainEntityIds, repository, customErrorMessage, paramName),
                haltOnFailure);
        }

        /// <summary>
        /// Overload for async validation func to check multiple repository item existence without retrieving data.
        /// </summary>
        public static async Task<OperationResult> ValidateAsync<TDomainEntity>(
            this OperationResult result,
            IEnumerable<int> domainEntityIds,
            Func<OperationResult, IEnumerable<int>, IBaseRepository<TDomainEntity>, string, string, Task<bool>> validationFunc,
            IBaseRepository<TDomainEntity> repository,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(domainEntityIds))] string paramName = null)
            where TDomainEntity : class, IIdentity
        {
            return await ValidationExtensions.ValidateInternalAsync(
                result,
                () => validationFunc(result, domainEntityIds, repository, customErrorMessage, paramName),
                haltOnFailure);
        }

        /// <summary>
        /// Overload for validation func to check repository item existence and fetch data using default GetByIdAsync.
        /// </summary>
        public static OperationResult Validate<TDomainEntity>(
            this OperationResult result,
            int? domainEntityId,
            Func<OperationResult, int?, IBaseRepository<TDomainEntity>, Action<TDomainEntity>, string, string, bool> validationFunc,
            IBaseRepository<TDomainEntity> repository,
            out TDomainEntity domainEntity,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(domainEntityId))] string paramName = null)
            where TDomainEntity : class, IIdentity
        {
            TDomainEntity tempDomainEntity = null;

            var validationResult = ValidationExtensions.ValidateInternal(
                result,
                () => validationFunc(result, domainEntityId, repository, x => tempDomainEntity = x, customErrorMessage, paramName),
                haltOnFailure);

            domainEntity = tempDomainEntity;

            return validationResult;
        }

        /// <summary>
        /// Overload for async validation func to check repository item existence and fetch data using default GetByIdAsync.
        /// </summary>
        public static async Task<OperationResult> ValidateAsync<TDomainEntity>(
            this OperationResult result,
            int? domainEntityId,
            Func<OperationResult, int?, IBaseRepository<TDomainEntity>, Action<TDomainEntity>, string, string, Task<bool>> validationFunc,
            IBaseRepository<TDomainEntity> repository,
            Action<TDomainEntity> domainEntitySetter,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(domainEntityId))] string paramName = null)
            where TDomainEntity : class, IIdentity
        {
            TDomainEntity tempDomainEntity = null;

            var validationResult = await ValidationExtensions.ValidateInternalAsync(
                result,
                () => validationFunc(result, domainEntityId, repository, x => tempDomainEntity = x, customErrorMessage, paramName),
                haltOnFailure);

            domainEntitySetter?.Invoke(tempDomainEntity);

            return validationResult;
        }

        /// <summary>
        /// Overload for validation func to check repository item existence and fetch data using a custom GetByIdAsync function.
        /// </summary>
        public static OperationResult Validate<TDomainEntity, TRepository>(
            this OperationResult result,
            int? domainEntityId,
            Func<OperationResult, int?, TRepository, Func<TRepository, int, Task<TDomainEntity>>, Action<TDomainEntity>, string, string, bool> validationFunc,
            TRepository repository,
            Func<TRepository, int, Task<TDomainEntity>> getByIdFuncAsync,
            out TDomainEntity domainEntity,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(domainEntityId))] string paramName = null)
            where TDomainEntity : class, IIdentity
            where TRepository : IBaseRepository<TDomainEntity>
        {
            TDomainEntity tempDomainEntity = null;

            var validationResult = ValidationExtensions.ValidateInternal(
                result,
                () => validationFunc(result, domainEntityId, repository, getByIdFuncAsync, x => tempDomainEntity = x, customErrorMessage, paramName),
                haltOnFailure);

            domainEntity = tempDomainEntity;

            return validationResult;
        }

        /// <summary>
        /// Overload for async validation func to check repository item existence and fetch data using a custom GetByIdAsync function.
        /// </summary>
        public static async Task<OperationResult> ValidateAsync<TDomainEntity, TRepository>(
            this OperationResult result,
            int? domainEntityId,
            Func<OperationResult, int?, TRepository, Func<TRepository, int, Task<TDomainEntity>>, Action<TDomainEntity>, string, string, Task<bool>> validationFunc,
            TRepository repository,
            Func<TRepository, int, Task<TDomainEntity>> getByIdFuncAsync,
            Action<TDomainEntity> domainEntitySetter,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(domainEntityId))] string paramName = null)
            where TDomainEntity : class, IIdentity
            where TRepository : IBaseRepository<TDomainEntity>
        {
            TDomainEntity tempDomainEntity = null;

            var validationResult = await ValidationExtensions.ValidateInternalAsync(
                result,
                () => validationFunc(result, domainEntityId, repository, getByIdFuncAsync, x => tempDomainEntity = x, customErrorMessage, paramName),
                haltOnFailure);

            domainEntitySetter?.Invoke(tempDomainEntity);

            return validationResult;
        }

        /// <summary>
        /// Overload for validation func to check multiple repository items existence at once, and fetch, using the default repository method.
        /// </summary>
        public static OperationResult Validate<TDomainEntity>(
            this OperationResult result,
            IEnumerable<int> domainEntityIds,
            Func<OperationResult, IEnumerable<int>, IBaseRepository<TDomainEntity>, Action<List<TDomainEntity>>, string, string, bool> validationFunc,
            IBaseRepository<TDomainEntity> repository,
            out List<TDomainEntity> domainEntities,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(domainEntityIds))] string paramName = null)
            where TDomainEntity : class, IIdentity
        {
            List<TDomainEntity> tempDomainEntities = null;

            var validationResult = ValidationExtensions.ValidateInternal(
                result,
                () => validationFunc(result, domainEntityIds, repository, x => tempDomainEntities = x, customErrorMessage, paramName),
                haltOnFailure);

            domainEntities = tempDomainEntities;

            return validationResult;
        }

        /// <summary>
        /// Overload for validation func to check multiple repository items existence at once, and fetch, using a custom repository method.
        /// </summary>
        public static OperationResult Validate<TDomainEntity>(
            this OperationResult result,
            IEnumerable<int> domainEntityIds,
            Func<OperationResult, IEnumerable<int>, IBaseRepository<TDomainEntity>, Func<IEnumerable<int>, Task<List<TDomainEntity>>>, Action<List<TDomainEntity>>, string, string, bool> validationFunc,
            IBaseRepository<TDomainEntity> repository,
            Func<IEnumerable<int>, Task<List<TDomainEntity>>> listByIdsFuncAsync,
            out List<TDomainEntity> domainEntities,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(domainEntityIds))] string paramName = null)
            where TDomainEntity : class, IIdentity
        {
            List<TDomainEntity> tempDomainEntities = null;

            var validationResult = ValidationExtensions.ValidateInternal(
                result,
                () => validationFunc(result, domainEntityIds, repository, listByIdsFuncAsync, x => tempDomainEntities = x, customErrorMessage, paramName),
                haltOnFailure);

            domainEntities = tempDomainEntities;

            return validationResult;
        }

        /// <summary>
        /// Helper for domain entity properties which need to return a value in a getter, but you want to validate a dependent property exists and throw
        /// a friendly exception if not.
        /// </summary>
        public static T ValidateDependentRelationshipIsIncludedAndReturnOrThrow<T>(
            this OperationResult result,
            T dependentRelationship,
            string customErrorMessage = null)
        {
            result
                .Validate(
                    param: (object)dependentRelationship,
                    validationFunc: DependentRelationshipIsIncluded,
                    customErrorMessage: customErrorMessage)
                .ThrowIfNotSuccessful();

            return dependentRelationship;
        }

        #endregion

        #region OperationResult<TResult> extensions

        /// <summary>
        /// Overload for validation func <see cref="DependentRelationshipIsIncludedUsingChangeTracker{TDomainEntity, TRelationship}(OperationResult, TDomainEntity, IUnitOfWork, Expression{Func{TDomainEntity, TRelationship}}, string, string)"/> for a one-to-one relationship.
        /// </summary>
        public static OperationResult<TResult> Validate<TResult, TDomainEntity, TRelationship>(
            this OperationResult<TResult> result,
            TDomainEntity param,
            Func<OperationResult, TDomainEntity, IUnitOfWork, Expression<Func<TDomainEntity, TRelationship>>, string, string, bool> validationFunc,
            IUnitOfWork unitOfWork,
            Expression<Func<TDomainEntity, TRelationship>> navigationPropertyAccessor,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(navigationPropertyAccessor))] string navigationPropertyAccessorName = null)
            where TDomainEntity : DomainEntityBase
            where TRelationship : DomainEntityBase
        {
            return (OperationResult<TResult>)((OperationResult)result).Validate(param, validationFunc, unitOfWork, navigationPropertyAccessor, customErrorMessage, haltOnFailure, navigationPropertyAccessorName);
        }

        /// <summary>
        /// Overload for validation func <see cref="DependentRelationshipIsIncludedUsingChangeTracker{TDomainEntity, TRelationship}(OperationResult, TDomainEntity, IUnitOfWork, Expression{Func{TDomainEntity, TRelationship}}, string, string)"/> for a one-to-many relationship.
        /// </summary>
        public static OperationResult<TResult> Validate<TResult, TDomainEntity, TRelationship>(
            this OperationResult<TResult> result,
            TDomainEntity param,
            Func<OperationResult, TDomainEntity, IUnitOfWork, Expression<Func<TDomainEntity, IEnumerable<TRelationship>>>, string, string, bool> validationFunc,
            IUnitOfWork unitOfWork,
            Expression<Func<TDomainEntity, IEnumerable<TRelationship>>> navigationPropertyAccessor,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(navigationPropertyAccessor))] string navigationPropertyAccessorName = null)
            where TDomainEntity : DomainEntityBase
            where TRelationship : DomainEntityBase
        {
            return (OperationResult<TResult>)((OperationResult)result).Validate(param, validationFunc, unitOfWork, navigationPropertyAccessor, customErrorMessage, haltOnFailure, navigationPropertyAccessorName);
        }

        /// <summary>
        /// Overload for validation func to check dependent relationship exists for all parent items.
        /// </summary>
        public static OperationResult<TResult> Validate<TResult, TParam>(
            this OperationResult<TResult> result,
            IEnumerable<TParam> param,
            Func<OperationResult, IEnumerable<TParam>, Func<TParam, object>, string, string, string, bool> validationFunc,
            Func<TParam, object> subDependentSelector,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(param))] string paramName = null,
            [CallerArgumentExpression(nameof(subDependentSelector))] string subDependentSelectorName = null)
            where TParam : class, IIdentity
        {
            return (OperationResult<TResult>)((OperationResult)result).Validate(param, validationFunc, subDependentSelector, customErrorMessage, haltOnFailure, paramName, subDependentSelectorName);
        }

        /// <summary>
        /// Overload for validation func to check dependent collection item existence and return data.
        /// </summary>
        public static OperationResult<TResult> Validate<TResult, TDomainEntity>(
            this OperationResult<TResult> result,
            IEnumerable<TDomainEntity> collection,
            Func<OperationResult, IEnumerable<TDomainEntity>, int, Action<TDomainEntity>, string, string, bool> validationFunc,
            int domainEntityId,
            out TDomainEntity domainEntity,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(collection))] string paramName = null)
            where TDomainEntity : class, IIdentity
        {
            return (OperationResult<TResult>)((OperationResult)result).Validate(collection, validationFunc, domainEntityId, out domainEntity, customErrorMessage, haltOnFailure, paramName);
        }

        /// <summary>
        /// Overload for validation func to check repository item existence without retrieving data.
        /// </summary>
        public static OperationResult<TResult> Validate<TResult, TDomainEntity>(
            this OperationResult<TResult> result,
            int? domainEntityId,
            Func<OperationResult, int?, IBaseRepository<TDomainEntity>, string, string, bool> validationFunc,
            IBaseRepository<TDomainEntity> repository,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(domainEntityId))] string paramName = null)
            where TDomainEntity : class, IIdentity
        {
            return (OperationResult<TResult>)((OperationResult)result).Validate(domainEntityId, validationFunc, repository, customErrorMessage, haltOnFailure, paramName);
        }

        /// <summary>
        /// Overload for async validation func to check repository item existence without retrieving data.
        /// </summary>
        public static async Task<OperationResult<TResult>> ValidateAsync<TResult, TDomainEntity>(
            this OperationResult<TResult> result,
            int? domainEntityId,
            Func<OperationResult, int?, IBaseRepository<TDomainEntity>, string, string, Task<bool>> validationFunc,
            IBaseRepository<TDomainEntity> repository,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(domainEntityId))] string paramName = null)
            where TDomainEntity : class, IIdentity
        {
            return (OperationResult<TResult>)await ((OperationResult)result).ValidateAsync(domainEntityId, validationFunc, repository, customErrorMessage, haltOnFailure, paramName);
        }

        /// <summary>
        /// Overload for validation func to check multiple repository item existence without retrieving data.
        /// </summary>
        public static OperationResult<TResult> Validate<TResult, TDomainEntity>(
            this OperationResult<TResult> result,
            IEnumerable<int> domainEntityIds,
            Func<OperationResult, IEnumerable<int>, IBaseRepository<TDomainEntity>, string, string, bool> validationFunc,
            IBaseRepository<TDomainEntity> repository,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(domainEntityIds))] string paramName = null)
            where TDomainEntity : class, IIdentity
        {
            return (OperationResult<TResult>)((OperationResult)result).Validate(domainEntityIds, validationFunc, repository, customErrorMessage, haltOnFailure, paramName);
        }

        /// <summary>
        /// Overload for async validation func to check multiple repository item existence without retrieving data.
        /// </summary>
        public static async Task<OperationResult<TResult>> ValidateAsync<TResult, TDomainEntity>(
            this OperationResult<TResult> result,
            IEnumerable<int> domainEntityIds,
            Func<OperationResult, IEnumerable<int>, IBaseRepository<TDomainEntity>, string, string, Task<bool>> validationFunc,
            IBaseRepository<TDomainEntity> repository,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(domainEntityIds))] string paramName = null)
            where TDomainEntity : class, IIdentity
        {
            return (OperationResult<TResult>)await ((OperationResult)result).ValidateAsync(domainEntityIds, validationFunc, repository, customErrorMessage, haltOnFailure, paramName);
        }

        /// <summary>
        /// Overload for validation func to check repository item existence and fetch data using default GetByIdAsync.
        /// </summary>
        public static OperationResult<TResult> Validate<TResult, TDomainEntity>(
            this OperationResult<TResult> result,
            int? domainEntityId,
            Func<OperationResult, int?, IBaseRepository<TDomainEntity>, Action<TDomainEntity>, string, string, bool> validationFunc,
            IBaseRepository<TDomainEntity> repository,
            out TDomainEntity domainEntity,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(domainEntityId))] string paramName = null)
            where TDomainEntity : class, IIdentity
        {
            return (OperationResult<TResult>)((OperationResult)result).Validate(domainEntityId, validationFunc, repository, out domainEntity, customErrorMessage, haltOnFailure, paramName);
        }

        /// <summary>
        /// Overload for async validation func to check repository item existence and fetch data using default GetByIdAsync.
        /// </summary>
        public static async Task<OperationResult<TResult>> ValidateAsync<TResult, TDomainEntity>(
            this OperationResult<TResult> result,
            int? domainEntityId,
            Func<OperationResult, int?, IBaseRepository<TDomainEntity>, Action<TDomainEntity>, string, string, Task<bool>> validationFunc,
            IBaseRepository<TDomainEntity> repository,
            Action<TDomainEntity> domainEntitySetter,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(domainEntityId))] string paramName = null)
            where TDomainEntity : class, IIdentity
        {
            return (OperationResult<TResult>)await ((OperationResult)result).ValidateAsync(domainEntityId, validationFunc, repository, domainEntitySetter, customErrorMessage, haltOnFailure, paramName);
        }

        /// <summary>
        /// Overload for validation func to check repository item existence and fetch data using a custom GetByIdAsync function.
        /// </summary>
        public static OperationResult<TResult> Validate<TResult, TDomainEntity, TRepository>(
            this OperationResult<TResult> result,
            int? domainEntityId,
            Func<OperationResult, int?, TRepository, Func<TRepository, int, Task<TDomainEntity>>, Action<TDomainEntity>, string, string, bool> validationFunc,
            TRepository repository,
            Func<TRepository, int, Task<TDomainEntity>> getByIdFuncAsync,
            out TDomainEntity domainEntity,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(domainEntityId))] string paramName = null)
            where TDomainEntity : class, IIdentity
            where TRepository : IBaseRepository<TDomainEntity>
        {
            return (OperationResult<TResult>)((OperationResult)result).Validate(domainEntityId, validationFunc, repository, getByIdFuncAsync, out domainEntity, customErrorMessage, haltOnFailure, paramName);
        }

        /// <summary>
        /// Overload for async validation func to check repository item existence and fetch data using a custom GetByIdAsync function.
        /// </summary>
        public static async Task<OperationResult<TResult>> ValidateAsync<TResult, TDomainEntity, TRepository>(
            this OperationResult<TResult> result,
            int? domainEntityId,
            Func<OperationResult, int?, TRepository, Func<TRepository, int, Task<TDomainEntity>>, Action<TDomainEntity>, string, string, Task<bool>> validationFunc,
            TRepository repository,
            Func<TRepository, int, Task<TDomainEntity>> getByIdFuncAsync,
            Action<TDomainEntity> domainEntitySetter,
            string customErrorMessage = null,
            bool haltOnFailure = true,
            [CallerArgumentExpression(nameof(domainEntityId))] string paramName = null)
            where TDomainEntity : class, IIdentity
            where TRepository : IBaseRepository<TDomainEntity>
        {
            return (OperationResult<TResult>)await ((OperationResult)result).ValidateAsync(domainEntityId, validationFunc, repository, getByIdFuncAsync, domainEntitySetter, customErrorMessage, haltOnFailure, paramName);
        }

        #endregion

        #region Validation functions

        /// <summary>
        /// Checks if the given dependent relationship is included by checking whether it is null or not.
        /// Works for most one-many cases (except when using SQLite which leaves the relationship null instead of Count = 0).
        /// Does not work for one-one cases, since there is no way to differentiate between related entity not included, or does not yet exist (would be null in both cases).
        /// </summary>
        public static bool DependentRelationshipIsIncluded(
            this OperationResult result,
            object param,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
        {
            if (param == null)
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"Dependent relationship {ValidationExtensions.FormatParamName(paramName)} must be included", OperationResultErrorType.EntityNotFoundOrUnauthorized);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks whether the given dependent relationship is included by using DbContext directly for a one-to-one relationship. Should be 100% reliable.
        /// </summary>
        public static bool DependentRelationshipIsIncludedUsingChangeTracker<TDomainEntity, TRelationship>(
            this OperationResult result,
            TDomainEntity param,
            IUnitOfWork unitOfWork,
            Expression<Func<TDomainEntity, TRelationship>> navigationPropertyAccessor,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(navigationPropertyAccessor))] string navigationPropertyAccessorName = null)
            where TDomainEntity : DomainEntityBase
            where TRelationship : DomainEntityBase
        {
            if (!unitOfWork.IsRelationshipLoaded(param, navigationPropertyAccessor))
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"Dependent relationship {ValidationExtensions.FormatParamName(navigationPropertyAccessorName)} must be included", OperationResultErrorType.EntityNotFoundOrUnauthorized);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks whether the given dependent relationship is included by using DbContext directly for a one-to-many relationship. Should be 100% reliable.
        /// </summary>
        public static bool DependentRelationshipIsIncludedUsingChangeTracker<TDomainEntity, TRelationship>(
            this OperationResult result,
            TDomainEntity param,
            IUnitOfWork unitOfWork,
            Expression<Func<TDomainEntity, IEnumerable<TRelationship>>> navigationPropertyAccessor,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(navigationPropertyAccessor))] string navigationPropertyAccessorName = null)
            where TDomainEntity : DomainEntityBase
            where TRelationship : DomainEntityBase
        {
            if (param == null
                || !unitOfWork.IsRelationshipLoaded(param, navigationPropertyAccessor))
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"Dependent relationship {ValidationExtensions.FormatParamName(navigationPropertyAccessorName)} must be included", OperationResultErrorType.EntityNotFoundOrUnauthorized);
                return false;
            }

            return true;
        }

        public static bool DependentRelationshipIsIncludedForAll<TParam>(
            this OperationResult result,
            IEnumerable<TParam> param,
            Func<TParam, object> subDependentSelector,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null,
            [CallerArgumentExpression(nameof(param))] string subDependentSelectorName = null)
            where TParam : class, IIdentity
        {
            param.ThrowIfNull();
            subDependentSelector.ThrowIfNull();

            if (param == null)
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"Dependent relationship {ValidationExtensions.FormatParamName(paramName)} must be included", OperationResultErrorType.EntityNotFoundOrUnauthorized);
                return false;
            }

            foreach (var item in param)
            {
                if (subDependentSelector(item) == null)
                {
                    result.IsSuccessful = false;
                    result.AddError(customErrorMessage ?? $"Dependent relationship {ValidationExtensions.FormatParamName(subDependentSelectorName)} must be included for all {ValidationExtensions.FormatParamName(paramName)}", OperationResultErrorType.EntityNotFoundOrUnauthorized);
                    return false;
                }
            }

            return true;
        }

        public static bool CheckDependentCollectionItemExistsAndFetch<TDomainEntity>(
            this OperationResult result,
            IEnumerable<TDomainEntity> param,
            int id,
            Action<TDomainEntity> domainEntitySetter,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(param))] string paramName = null)
            where TDomainEntity : IIdentity
        {
            param.ThrowIfNull(paramName: paramName);

            var item = param.SingleOrDefault(x => x.Id == id);

            domainEntitySetter(item);

            if (item == null)
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{ValidationExtensions.FormatParamName(paramName)} does not contain an item with {nameof(IIdentity.Id)} {id}", OperationResultErrorType.EntityNotFoundOrUnauthorized);
                return false;
            }

            return true;
        }

        public static bool CheckRepositoryItemExists<TDomainEntity>(
            this OperationResult result,
            int? domainEntityId,
            IBaseRepository<TDomainEntity> repository,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(domainEntityId))] string paramName = null)
            where TDomainEntity : class, IIdentity
        {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits - required
            return Task.Run(async () => await CheckRepositoryItemExistsAsync(result, domainEntityId, repository, customErrorMessage, paramName)).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
        }

        public static async Task<bool> CheckRepositoryItemExistsAsync<TDomainEntity>(
            this OperationResult result,
            int? domainEntityId,
            IBaseRepository<TDomainEntity> repository,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(domainEntityId))] string paramName = null)
            where TDomainEntity : class, IIdentity
        {
            if (domainEntityId == null)
            {
                return true;
            }

            var exists = await repository.ExistsAsync(domainEntityId.Value);

            if (!exists)
            {
                // a specific int value was passed as the param, so change the param name to "Id" for display purposes
                if (int.TryParse(paramName, out _))
                {
                    paramName = "Id";
                }

                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{typeof(TDomainEntity).Name} with {ValidationExtensions.FormatParamName(paramName)} {domainEntityId} does not exist", OperationResultErrorType.EntityNotFoundOrUnauthorized);
                return false;
            }

            return true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Required")]
        public static bool CheckRepositoryItemsExist<TDomainEntity>(
            this OperationResult result,
            IEnumerable<int> domainEntityIds,
            IBaseRepository<TDomainEntity> repository,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(domainEntityIds))] string paramName = null)
            where TDomainEntity : class, IIdentity
        {
            return Task.Run(async () => await CheckRepositoryItemsExistAsync(result, domainEntityIds, repository, customErrorMessage, paramName)).GetAwaiter().GetResult();
        }

        public static async Task<bool> CheckRepositoryItemsExistAsync<TDomainEntity>(
            this OperationResult result,
            IEnumerable<int> domainEntityIds,
            IBaseRepository<TDomainEntity> repository,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(domainEntityIds))] string paramName = null)
            where TDomainEntity : class, IIdentity
        {
            if (!domainEntityIds.Any())
            {
                return true;
            }

            var allExist = await repository.ExistsAsync(domainEntityIds);

            if (!allExist)
            {
                // a specific list of int values was passed as the param, so change the param name to "Ids" for display purposes
                if (paramName.Split(",").Select(x => x.Trim()).All(x => int.TryParse(x, out _)))
                {
                    paramName = "Ids";
                }

                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"Not all {typeof(TDomainEntity).Name}(s) for the given {ValidationExtensions.FormatParamName(paramName)} exist", OperationResultErrorType.EntityNotFoundOrUnauthorized);
                return false;
            }

            return true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Required")]
        public static bool CheckRepositoryItemExistsAndFetch<TDomainEntity>(
            this OperationResult result,
            int? domainEntityId,
            IBaseRepository<TDomainEntity> repository,
            Action<TDomainEntity> domainEntitySetter,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(domainEntityId))] string paramName = null)
            where TDomainEntity : class, IIdentity
        {
            return Task.Run(async () => await CheckRepositoryItemExistsAndFetchAsync(
                result,
                domainEntityId,
                repository,
                domainEntitySetter,
                customErrorMessage,
                paramName)).GetAwaiter().GetResult();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Required")]
        public static bool CheckRepositoryItemExistsAndFetch<TDomainEntity, TRepository>(
            this OperationResult result,
            int? domainEntityId,
            TRepository repository,
            Func<TRepository, int, Task<TDomainEntity>> getByIdFuncAsync,
            Action<TDomainEntity> domainEntitySetter,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(domainEntityId))] string paramName = null)
            where TDomainEntity : class, IIdentity
            where TRepository : IBaseRepository<TDomainEntity>
        {
            return Task.Run(async () => await CheckRepositoryItemExistsAndFetchAsync(
                result,
                domainEntityId,
                repository,
                getByIdFuncAsync,
                domainEntitySetter,
                customErrorMessage,
                paramName)).GetAwaiter().GetResult();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Required")]
        public static bool CheckRepositoryItemsExistAndFetch<TDomainEntity>(
            this OperationResult result,
            IEnumerable<int> domainEntityIds,
            IBaseRepository<TDomainEntity> repository,
            Action<List<TDomainEntity>> domainEntitiesSetter,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(domainEntityIds))] string paramName = null)
            where TDomainEntity : class, IIdentity
        {
            return Task.Run(async () => await CheckRepositoryItemsExistAndFetchAsync(
                result,
                domainEntityIds,
                repository.ListByIdListAsync,
                domainEntitiesSetter,
                customErrorMessage,
                paramName)).GetAwaiter().GetResult();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Required")]
        public static bool CheckRepositoryItemsExistAndFetch<TDomainEntity>(
            this OperationResult result,
            IEnumerable<int> domainEntityIds,
            Func<IEnumerable<int>, Task<List<TDomainEntity>>> listByIdsFuncAsync,
            Action<List<TDomainEntity>> domainEntitiesSetter,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(domainEntityIds))] string paramName = null)
            where TDomainEntity : class, IIdentity
        {
            return Task.Run(async () => await CheckRepositoryItemsExistAndFetchAsync(
                result,
                domainEntityIds,
                listByIdsFuncAsync,
                domainEntitiesSetter,
                customErrorMessage,
                paramName)).GetAwaiter().GetResult();
        }

        public static async Task<bool> CheckRepositoryItemExistsAndFetchAsync<TDomainEntity, TRepository>(
            this OperationResult result,
            int? domainEntityId,
            TRepository repository,
            Action<TDomainEntity> domainEntitySetter,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(domainEntityId))] string paramName = null)
            where TDomainEntity : class, IIdentity
            where TRepository : IBaseRepository<TDomainEntity>
        {
            return await result.CheckRepositoryItemExistsAndFetchAsync(
                domainEntityId,
                repository,
                (repository, id) => repository.GetByIdAsync(id),
                domainEntitySetter,
                customErrorMessage,
                paramName);
        }

        public static async Task<bool> CheckRepositoryItemExistsAndFetchAsync<TDomainEntity, TRepository>(
            this OperationResult result,
            int? domainEntityId,
            TRepository repository,
            Func<TRepository, int, Task<TDomainEntity>> getByIdFuncAsync,
            Action<TDomainEntity> domainEntitySetter,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(domainEntityId))] string paramName = null)
            where TDomainEntity : class, IIdentity
            where TRepository : IBaseRepository<TDomainEntity>
        {
            if (domainEntityId == null)
            {
                return true;
            }

            var domainEntity = await getByIdFuncAsync(repository, domainEntityId.Value);

            domainEntitySetter(domainEntity);

            if (domainEntity == null)
            {
                // a specific int value was passed as the param, so change the param name to "Id" for display purposes
                if (int.TryParse(paramName, out _))
                {
                    paramName = "id";
                }

                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{typeof(TDomainEntity).Name} with {ValidationExtensions.FormatParamName(paramName)} {domainEntityId} does not exist", OperationResultErrorType.EntityNotFoundOrUnauthorized);
                return false;
            }

            return true;
        }

        public static async Task<bool> CheckRepositoryItemsExistAndFetchAsync<TDomainEntity>(
            this OperationResult result,
            IEnumerable<int> domainEntityIds,
            IBaseRepository<TDomainEntity> repository,
            Action<List<TDomainEntity>> domainEntitiesSetter,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(domainEntityIds))] string paramName = null)
            where TDomainEntity : class, IIdentity
        {
            return await result.CheckRepositoryItemsExistAndFetchAsync(
                domainEntityIds,
                repository.ListByIdListAsync,
                domainEntitiesSetter,
                customErrorMessage,
                paramName);
        }

        public static async Task<bool> CheckRepositoryItemsExistAndFetchAsync<TDomainEntity>(
            this OperationResult result,
            IEnumerable<int> domainEntityIds,
            Func<IEnumerable<int>, Task<List<TDomainEntity>>> listByIdsFuncAsync,
            Action<List<TDomainEntity>> domainEntitiesSetter,
            string customErrorMessage = null,
            [CallerArgumentExpression(nameof(domainEntityIds))] string paramName = null)
            where TDomainEntity : class, IIdentity
        {
            if (domainEntityIds == null)
            {
                return true;
            }

            var domainEntities = await listByIdsFuncAsync(domainEntityIds);

            domainEntitiesSetter(domainEntities);

            domainEntities ??= new List<TDomainEntity>();
            var missingIds = domainEntityIds.Except(domainEntities.Select(x => x.Id ) ).ToList();

            if (missingIds.Any())
            {
                result.IsSuccessful = false;
                result.AddError(customErrorMessage ?? $"{typeof(TDomainEntity).Name}(s) with {ValidationExtensions.FormatParamName(paramName)} {missingIds.StringJoin(",")} do not exist", OperationResultErrorType.EntityNotFoundOrUnauthorized);
                return false;
            }

            return true;
        }

        #endregion

        #region Sample code
        /* private static OperationResult<string> Example(
            object obj,
            string str,
            Person person,
            List<int> intList,
            int someInt,
            int personId,
            IBaseRepository<Person> personRepo)
        {
            var result = new OperationResult<string>(); // default IsSuccessful = true, Data is null

            result
                .Validate(obj, IsNotNull, haltOnFailure: false) // don't halt if this fails, keep validating
                .Validate(str, StringIsNotNullOrWhiteSpace) // would halt here without haltOnFailure: false
                .Validate(person.Address, DependentRelationshipIsIncluded, customErrorMessage: $"Address must be included!") // custom error message
                .Validate(intList, ListIsNotNullOrEmpty, paramName: "List of numbers") // substitute a custom parameter name into auto-generated error message
                .Validate(intList, ListIsEmpty)
                .Validate(intList, ListContains, 1)
                .Validate(str, IsTrue, x => x.Contains("hello")) // for when a more complex expression is required
                .Validate(str, IsFalse, x => x.Contains("hello")) // for when a more complex expression is required
                .Validate(someInt, IsGreaterThan, 0)
                .Validate(someInt, IsBetween, 10, 20)
                .Validate<Person>(personId, CheckRepositoryItemExists, personRepo) // validates the existence of a record from the db
                // validates the existence of a record from the db and fetches it for later use
                .Validate(personId, CheckRepositoryItemExistsAndFetch, personRepo, out var fetchedPerson);

            if (!result.IsSuccessful)
            {
                return result;
            }

            result.Data = "I succeeded!"; // set Data

            return result;
        }

        internal class Person : IIdentity
        {
            public int Id { get; set; }

            internal Address Address { get; set; }
        }

        internal class Address
        {
        } */
        #endregion
    }
}