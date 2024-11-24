using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using AutoMapper;
using Common.Exceptions;
using Common.ExtensionMethods;
using Common.Models;
using Common.Utility;
using Items.Data.EFCore;
using Items.Data.EFCore.Abstraction.Interfaces;
using Items.Data.EFCore.Entities;
using Items.Data.EFCore.Entities.Interfaces;
using Items.Data.EFCore.Utility;
using Items.GenericServices.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pluralize.NET;
using Serilog.Context;
using Serilog.Core;

namespace Items.GenericServices
{
    public abstract class GenericBusinessLogic<TDomainEntity, TUnitOfWork, TRepository> : IGenericBusinessLogic<TDomainEntity, TUnitOfWork, TRepository>
        where TDomainEntity : class, IIdentity
        where TUnitOfWork : class, IUnitOfWork
        where TRepository : class, IBaseRepository<TDomainEntity>
    {
        private static readonly Pluralizer Pluralizer = new Pluralizer();

        protected GenericBusinessLogic(
            TUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger logger)
            : this(unitOfWork, mapper, logger, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericBusinessLogic{TDomainEntity, TUnitOfWork, TRepository}"/> class.
        /// Overload to pass in an IHttpContextAccessor, which can be used in conjunction with a custom logging action.
        /// </summary>
        protected GenericBusinessLogic(
            TUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger logger,
            IHttpContextAccessor httpContextAccessor)
        {
            this.UnitOfWork = unitOfWork;
            this.Mapper = mapper;
            this.Logger = logger;
            this.HttpContextAccessor = httpContextAccessor;
        }

        public abstract TRepository Repository { get; }

        protected TUnitOfWork UnitOfWork { get; }

        protected IMapper Mapper { get; }

        protected ILogger Logger { get; }

        protected IHttpContextAccessor HttpContextAccessor { get; }

        private static string EntityTypeName => typeof(TDomainEntity).Name;

        public async Task<OperationResult<TDomainEntity>> GetByIdAsync(int id)
        {
            return await this.GetByIdInternalAsync(id, null);
        }

        public async Task<OperationResult<TDomainEntity>> GetByIdAsync(int id, Func<TRepository, int, Task<TDomainEntity>> lookupFuncAsync)
        {
            return await this.GetByIdInternalAsync(
                id: id,
                lookupFuncAsync: (id) => lookupFuncAsync(this.Repository, id));
        }

        public async Task<OperationResult<TOutputDTO>> GetByIdAndMapAsync<TOutputDTO>(int id, Func<TRepository, int, Task<TDomainEntity>> lookupFuncAsync = null)
            where TOutputDTO : class
        {
            var result = new OperationResult<TOutputDTO>();

            try
            {
                TDomainEntity entity;

                if (lookupFuncAsync != null)
                {
                    entity = await lookupFuncAsync(this.Repository, id);
                }
                else
                {
                    entity = await this.Repository.GetByIdAsync(id);
                }

                return this.HandleGetEntityResultAndMap(result, entity);
            }
            catch (Exception ex)
            {
                return (OperationResult<TOutputDTO>)await this.HandleExceptionResultAsync(result, ex, $"Error getting {EntityTypeName}");
            }
        }

        public async Task<OperationResult<TOutputDTO>> GetAndMapAsync<TOutputDTO>(Func<TRepository, Task<TDomainEntity>> lookupFuncAsync)
            where TOutputDTO : class
        {
            var result = new OperationResult<TOutputDTO>();

            try
            {
                TDomainEntity entity = await lookupFuncAsync(this.Repository);

                return this.HandleGetEntityResultAndMap(result, entity);
            }
            catch (Exception ex)
            {
                return (OperationResult<TOutputDTO>)await this.HandleExceptionResultAsync(result, ex, $"Error getting {EntityTypeName}");
            }
        }

        public async Task<OperationResult<TOutputDTO>> GetByIdAndMapAsync<TDomainEntityResult, TOutputDTO>(
            int id,
            Func<TRepository, int, Task<TDomainEntityResult>> lookupFuncAsync = null)
            where TDomainEntityResult : class
            where TOutputDTO : class
        {
            var result = new OperationResult<TOutputDTO>();

            try
            {
                TDomainEntityResult entity;

                if (lookupFuncAsync != null)
                {
                    entity = await lookupFuncAsync(this.Repository, id);
                }
                else
                {
                    entity = await this.Repository.GetByIdAsync(id) as TDomainEntityResult;
                }

                return this.HandleGetEntityResultAndMap(result, entity);
            }
            catch (Exception ex)
            {
                return (OperationResult<TOutputDTO>)await this.HandleExceptionResultAsync(result, ex, $"Error getting {typeof(TDomainEntityResult).Name}");
            }
        }

        public async Task<OperationResult<TOutputDTO>> GetByIdAndMapAsync<TDomainEntityResult, TOutputDTO>(
            int id,
            Func<TDomainEntity, TDomainEntityResult> entityPropertySelectorFunc,
            Func<TRepository, int, Task<TDomainEntity>> lookupFuncAsync = null)
            where TDomainEntityResult : class
            where TOutputDTO : class
        {
            var result = new OperationResult<TOutputDTO>();

            try
            {
                TDomainEntity entity;

                if (lookupFuncAsync != null)
                {
                    entity = await lookupFuncAsync(this.Repository, id);
                }
                else
                {
                    entity = await this.Repository.GetByIdAsync(id);
                }

                return this.HandleGetEntityResultAndMap(result, entity, entityPropertySelectorFunc);
            }
            catch (Exception ex)
            {
                return (OperationResult<TOutputDTO>)await this.HandleExceptionResultAsync(result, ex, $"Error getting {typeof(TDomainEntityResult).Name}");
            }
        }

        public async Task<OperationResult<List<TOutputDTO>>> ListAndMapAsync<TOutputDTO>(
            Func<TRepository, Task<List<TDomainEntity>>> listFunc = null)
            where TOutputDTO : class
        {
            var result = new OperationResult<List<TOutputDTO>>();

            try
            {
                var entities = listFunc == null ? await this.Repository.ListAsync() : await listFunc(this.Repository);

                if (entities == null)
                {
                    throw new InvalidOperationException("Entity list function returned null");
                }

                result.Data = this.Mapper.Map<List<TOutputDTO>>(entities);
                return result;
            }
            catch (Exception ex)
            {
                return (OperationResult<List<TOutputDTO>>)await this.HandleExceptionResultAsync(result, ex, $"Error listing {EntityTypeName}s");
            }
        }

        public async Task<OperationResult<TOutputDTO>> GetForSummaryOperationsAsync<TOutputDTO>(
            Func<TRepository, Task<TOutputDTO>> lookupFuncAsync)
            where TOutputDTO : class
        {
            var result = new OperationResult<TOutputDTO>();

            try
            {
                var summaryDTO = await lookupFuncAsync(this.Repository);

                if (summaryDTO == null)
                {
                    result.AddError(DomainEntityBase.GetNonExistentOrUnauthorisedEntityMessage<TDomainEntity>(), OperationResultErrorType.EntityNotFoundOrUnauthorized);
                    return result;
                }

                result.Data = summaryDTO;

                return result;
            }
            catch (Exception ex)
            {
                return (OperationResult<TOutputDTO>)await this.HandleExceptionResultAsync(result, ex, $"Error getting {EntityTypeName}");
            }
        }

        public async Task<OperationResult<List<TOutputDTO>>> ListAndMapAsync<TDomainEntityResult, TOutputDTO>(
            Func<TRepository, Task<List<TDomainEntityResult>>> listFuncAsync)
            where TDomainEntityResult : class
            where TOutputDTO : class
        {
            var result = new OperationResult<List<TOutputDTO>>();

            try
            {
                var entities = await listFuncAsync(this.Repository);

                if (entities == null)
                {
                    throw new InvalidOperationException("Entity list function returned null");
                }

                result.Data = this.Mapper.Map<List<TOutputDTO>>(entities);
                return result;
            }
            catch (Exception ex)
            {
                return (OperationResult<List<TOutputDTO>>)await this.HandleExceptionResultAsync(result, ex, $"Error listing {EntityTypeName}s");
            }
        }

        public async Task<OperationResult<TDomainEntity>> CreateSingleEntityAndSaveAsync(
            Func<TUnitOfWork, Task<OperationResult<TDomainEntity>>> entityCreationFuncAsync)
        {
            var result = new OperationResult<TDomainEntity>();

            try
            {
                var entityCreationResult = await entityCreationFuncAsync(this.UnitOfWork);

                if (!entityCreationResult.IsSuccessful)
                {
                    result.AddErrors(entityCreationResult);
                    await GenericBusinessLogic.TryRollbackResultAsync(entityCreationResult, result);
                    return result;
                }

                await this.Repository.AddAsync(entityCreationResult.Data);
                await this.SaveChangesAsync();

                result.Data = entityCreationResult.Data;

                return result;
            }
            catch (Exception ex)
            {
                return (OperationResult<TDomainEntity>)await this.HandleExceptionResultAsync(result, ex, $"Error creating {EntityTypeName}");
            }
        }

        public async Task<OperationResult<int>> CreateSingleEntitySaveAndReturnIdAsync(
            Func<TUnitOfWork, Task<OperationResult<TDomainEntity>>> entityCreationFuncAsync)
        {
            var result = new OperationResult<int>();

            try
            {
                var entityCreationResult = await entityCreationFuncAsync(this.UnitOfWork);

                if (!entityCreationResult.IsSuccessful)
                {
                    result.AddErrors(entityCreationResult);
                    await GenericBusinessLogic.TryRollbackResultAsync(entityCreationResult, result);
                    return result;
                }

                await this.Repository.AddAsync(entityCreationResult.Data);
                await this.SaveChangesAsync();

                result.Data = entityCreationResult.Data.Id;

                return result;
            }
            catch (Exception ex)
            {
                return (OperationResult<int>)await this.HandleExceptionResultAsync(result, ex, $"Error creating {EntityTypeName}");
            }
        }

        public async Task<OperationResult<List<TDomainEntity>>> CreateEntityListAndSaveAsync(
            Func<TUnitOfWork, Task<OperationResult<List<TDomainEntity>>>> entityCreationFuncAsync)
        {
            var result = new OperationResult<List<TDomainEntity>>();

            try
            {
                var entityListCreationResult = await entityCreationFuncAsync(this.UnitOfWork);

                if (!entityListCreationResult.IsSuccessful)
                {
                    result.AddErrors(entityListCreationResult);
                    await GenericBusinessLogic.TryRollbackResultAsync(entityListCreationResult, result);
                    return result;
                }

                foreach (var entity in entityListCreationResult.Data)
                {
                    await this.Repository.AddAsync(entity);
                }

                await this.SaveChangesAsync();

                result.Data = entityListCreationResult.Data;

                return result;
            }
            catch (Exception ex)
            {
                return (OperationResult<List<TDomainEntity>>)await this.HandleExceptionResultAsync(result, ex, $"Error creating {EntityTypeName}");
            }
        }

        public async Task<OperationResult<TOutputDTO>> CreateSingleEntitySaveAndMapAsync<TOutputDTO>(
            Func<TUnitOfWork, Task<OperationResult<TDomainEntity>>> entityCreationFuncAsync)
            where TOutputDTO : class
        {
            var result = new OperationResult<TOutputDTO>();

            try
            {
                var entityCreationResult = await entityCreationFuncAsync(this.UnitOfWork);

                if (!entityCreationResult.IsSuccessful)
                {
                    result.AddErrors(entityCreationResult);
                    await GenericBusinessLogic.TryRollbackResultAsync(entityCreationResult, result);
                    return result;
                }

                await this.Repository.AddAsync(entityCreationResult.Data);
                await this.SaveChangesAsync();

                result.Data = this.Mapper.Map<TOutputDTO>(entityCreationResult.Data);
                return result;
            }
            catch (Exception ex)
            {
                return (OperationResult<TOutputDTO>)await this.HandleExceptionResultAsync(result, ex, $"Error creating {EntityTypeName}");
            }
        }

        public async Task<OperationResult<TDomainEntityResult>> CallDomainMethodOnGivenEntityAsync<TDomainEntityResult>(
            TDomainEntity domainEntity,
            Func<TDomainEntity, TUnitOfWork, Task<OperationResult<TDomainEntityResult>>> domainMethodFuncAsync)
            where TDomainEntityResult : class
        {
            var result = new OperationResult<TDomainEntityResult>();

            try
            {
                var domainMethodResult = await domainMethodFuncAsync(domainEntity, this.UnitOfWork);
                return HandleDomainMethodResult(result, domainMethodResult);
            }
            catch (Exception ex)
            {
                return (OperationResult<TDomainEntityResult>)await this.HandleExceptionResultAsync(result, ex, $"Error running domain method on {EntityTypeName}");
            }
        }

        public async Task<OperationResult<List<TOutputDTO>>> CallDomainMethodOnGivenEntityAndMapAsync<TDomainEntityResult, TOutputDTO>(
            TDomainEntity domainEntity,
            Func<TDomainEntity, TUnitOfWork, Task<OperationResult<List<TDomainEntityResult>>>> domainMethodFuncAsync)
            where TDomainEntityResult : class
            where TOutputDTO : class
        {
            var result = new OperationResult<List<TOutputDTO>>();

            try
            {
                var domainMethodResult = await domainMethodFuncAsync(domainEntity, this.UnitOfWork);
                return this.HandleDomainMethodResultAndMap(result, domainMethodResult);
            }
            catch (Exception ex)
            {
                return (OperationResult<List<TOutputDTO>>)await this.HandleExceptionResultAsync(result, ex, $"Error running domain method on {EntityTypeName}");
            }
        }

        public async Task<OperationResult<TOutputDTO>> LookupEntityThenCallDomainMethodAndMapAsync<TDomainEntityResult, TOutputDTO>(
            int id,
            Func<TDomainEntity, TUnitOfWork, Task<OperationResult<TDomainEntityResult>>> domainMethodFuncAsync,
            Func<TRepository, int, Task<TDomainEntity>> lookupFuncAsync = null)
            where TDomainEntityResult : class
            where TOutputDTO : class
        {
            var result = new OperationResult<TOutputDTO>();

            try
            {
                var entity = await this.ValidateEntityExistenceAsync(id, result, lookupFuncAsync);

                if (!result.IsSuccessful)
                {
                    return result;
                }

                var domainMethodResult = await domainMethodFuncAsync(entity, this.UnitOfWork);

                return this.HandleDomainMethodResultAndMap(result, domainMethodResult);
            }
            catch (Exception ex)
            {
                return (OperationResult<TOutputDTO>)await this.HandleExceptionResultAsync(result, ex, $"Error running domain method on {EntityTypeName}");
            }
        }

        public async Task<OperationResult<List<TOutputDTO>>> LookupEntityThenCallDomainMethodAndMapAsync<TDomainEntityResult, TOutputDTO>(
            int id,
            Func<TDomainEntity, TUnitOfWork, Task<OperationResult<List<TDomainEntityResult>>>> domainMethodFuncAsync,
            Func<TRepository, int, Task<TDomainEntity>> lookupFuncAsync = null)
            where TDomainEntityResult : class
            where TOutputDTO : class
        {
            var result = new OperationResult<List<TOutputDTO>>();

            try
            {
                var entity = await this.ValidateEntityExistenceAsync(id, result, lookupFuncAsync);

                if (!result.IsSuccessful)
                {
                    return result;
                }

                var domainMethodResult = await domainMethodFuncAsync(entity, this.UnitOfWork);

                return this.HandleDomainMethodResultAndMap(result, domainMethodResult);
            }
            catch (Exception ex)
            {
                return (OperationResult<List<TOutputDTO>>)await this.HandleExceptionResultAsync(result, ex, $"Error running domain method on {EntityTypeName}");
            }
        }

        public async Task<OperationResult<TDomainEntityResult>> CallDomainMethodOnGivenEntityAndSaveAsync<TDomainEntityResult>(
            TDomainEntity domainEntity,
            Func<TDomainEntity, TUnitOfWork, Task<OperationResult<TDomainEntityResult>>> domainMethodFuncAsync)
            where TDomainEntityResult : class
        {
            return await GenericBusinessLogic.CallDomainMethodOnGivenEntityAndSaveAsync(
                domainEntity,
                domainMethodFuncAsync,
                this.Logger,
                this.UnitOfWork,
                this.HttpContextAccessor);
        }

        public async Task<OperationResult<TDomainEntityResult>> LookupEntityThenCallDomainMethodAsync<TDomainEntityResult>(
            int id,
            Func<TDomainEntity, TUnitOfWork, Task<OperationResult<TDomainEntityResult>>> domainMethodFuncAsync,
            Func<TRepository, int, Task<TDomainEntity>> lookupFuncAsync = null)
        {
            var result = new OperationResult<TDomainEntityResult>();

            try
            {
                var entity = await this.ValidateEntityExistenceAsync(id, result, lookupFuncAsync);

                if (!result.IsSuccessful)
                {
                    return result;
                }

                var domainMethodResult = await domainMethodFuncAsync(entity, this.UnitOfWork);

                return await this.HandleTypedDomainMethodResultAsync(result, domainMethodResult);
            }
            catch (Exception ex)
            {
                return (OperationResult<TDomainEntityResult>)await this.HandleExceptionResultAsync(result, ex, $"Error running domain method on {EntityTypeName}");
            }
        }

        public async Task<OperationResult<TDomainEntityResult>> LookupEntityThenCallDomainMethodAndSaveAsync<TDomainEntityResult>(
            int id,
            Func<TDomainEntity, TUnitOfWork, Task<OperationResult<TDomainEntityResult>>> domainMethodFuncAsync,
            Func<TRepository, int, Task<TDomainEntity>> lookupFuncAsync = null)
            where TDomainEntityResult : class
        {
            var result = new OperationResult<TDomainEntityResult>();

            try
            {
                var entity = await this.ValidateEntityExistenceAsync(id, result, lookupFuncAsync);

                if (!result.IsSuccessful)
                {
                    return result;
                }

                var domainMethodResult = await domainMethodFuncAsync(entity, this.UnitOfWork);

                return await this.HandleDomainMethodResultAndSaveChangesAsync(result, domainMethodResult);
            }
            catch (Exception ex)
            {
                return (OperationResult<TDomainEntityResult>)await this.HandleExceptionResultAsync(result, ex, $"Error running domain method on {EntityTypeName}");
            }
        }

        public async Task<OperationResult> CallDomainMethodOnGivenEntityAndSaveAsync(
            TDomainEntity domainEntity,
            Func<TDomainEntity, TUnitOfWork, Task<OperationResult>> domainMethodFuncAsync)
        {
            var result = new OperationResult<TDomainEntity>();

            try
            {
                var domainMethodResult = await domainMethodFuncAsync(domainEntity, this.UnitOfWork);
                await this.HandleDomainMethodResultAndSaveChangesAsync(result, domainMethodResult);
                return result;
            }
            catch (Exception ex)
            {
                return await this.HandleExceptionResultAsync(result, ex, $"Error running domain method on {EntityTypeName}");
            }
        }

        public async Task<OperationResult> LookupEntityThenCallDomainMethodAndSaveAsync(
            int id,
            Func<TDomainEntity, TUnitOfWork, Task<OperationResult>> domainMethodFuncAsync,
            Func<TRepository, int, Task<TDomainEntity>> lookupFuncAsync = null)
        {
            var result = new OperationResult<TDomainEntity>();

            try
            {
                var entity = await this.ValidateEntityExistenceAsync(id, result, lookupFuncAsync);

                if (!result.IsSuccessful)
                {
                    return result;
                }

                var domainMethodResult = await domainMethodFuncAsync(entity, this.UnitOfWork);

                await this.HandleDomainMethodResultAndSaveChangesAsync(result, domainMethodResult);

                return result;
            }
            catch (Exception ex)
            {
                return await this.HandleExceptionResultAsync(result, ex, $"Error running domain method on {EntityTypeName}");
            }
        }

        public async Task<OperationResult<List<OperationResult<TDomainEntity>>>> LookupEntityListThenCallDomainMethodOnEachAndSaveAsync(
            IEnumerable<int> ids,
            Func<TDomainEntity, TUnitOfWork, Task<OperationResult<TDomainEntity>>> domainMethodFuncAsync,
            Func<TRepository, IEnumerable<int>, Task<List<TDomainEntity>>> lookupFuncAsync)
        {
            var result = new OperationResult<List<OperationResult<TDomainEntity>>>();

            try
            {
                var entities = await this.ValidateMultipleEntityExistenceAsync(ids, result, lookupFuncAsync);

                if (!result.IsSuccessful)
                {
                    return result;
                }

                result.Data = new List<OperationResult<TDomainEntity>>();

                foreach (var entity in entities)
                {
                    var domainMethodResult = await domainMethodFuncAsync(entity, this.UnitOfWork);

                    await this.HandleDomainMethodResultAsync(result, domainMethodResult);

                    if (!result.IsSuccessful)
                    {
                        return result;
                    }

                    result.Data.Add(domainMethodResult);
                }

                await this.SaveChangesAsync();

                return result;
            }
            catch (Exception ex)
            {
                return (OperationResult<List<OperationResult<TDomainEntity>>>)await this.HandleExceptionResultAsync(result, ex, $"Error running domain method on {EntityTypeName}");
            }
        }

        public async Task<OperationResult<List<OperationResult>>> LookupEntityListThenCallDomainMethodOnEachAndSaveAsync(
            IEnumerable<int> ids,
            Func<TDomainEntity, TUnitOfWork, Task<OperationResult>> domainMethodFuncAsync,
            Func<TRepository, IEnumerable<int>, Task<List<TDomainEntity>>> lookupFuncAsync)
        {
            var result = new OperationResult<List<OperationResult>>();

            try
            {
                var entities = await this.ValidateMultipleEntityExistenceAsync(ids, result, lookupFuncAsync);

                if (!result.IsSuccessful)
                {
                    return result;
                }

                result.Data = new List<OperationResult>();

                foreach (var entity in entities)
                {
                    var domainMethodResult = await domainMethodFuncAsync(entity, this.UnitOfWork);

                    await this.HandleDomainMethodResultAsync(result, domainMethodResult);

                    if (!result.IsSuccessful)
                    {
                        return result;
                    }

                    result.Data.Add(domainMethodResult);
                }

                await this.SaveChangesAsync();

                return result;
            }
            catch (Exception ex)
            {
                return (OperationResult<List<OperationResult>>)await this.HandleExceptionResultAsync(result, ex, $"Error running domain method on {EntityTypeName}");
            }
        }

        public async Task<OperationResult<TOutputDTO>> LookupEntityThenCallDomainMethodSaveAndMapAsync<TOutputDTO>(
            int id,
            Func<TDomainEntity, TUnitOfWork, Task<OperationResult<TDomainEntity>>> domainMethodFuncAsync,
            Func<TRepository, int, Task<TDomainEntity>> lookupFuncAsync = null)
            where TOutputDTO : class
        {
            return await this.LookupEntityThenCallDomainMethodSaveAndMapAsync<TDomainEntity, TOutputDTO>(id, domainMethodFuncAsync, lookupFuncAsync);
        }

        public async Task<OperationResult<TOutputDTO>> CallDomainMethodOnGivenEntitySaveAndMapAsync<TDomainEntityResult, TOutputDTO>(
            TDomainEntity domainEntity,
            Func<TDomainEntity, TUnitOfWork, Task<OperationResult<TDomainEntityResult>>> domainMethodFuncAsync)
            where TDomainEntityResult : class, IIdentity
            where TOutputDTO : class
        {
            var result = new OperationResult<TOutputDTO>();

            try
            {
                var domainMethodResult = await domainMethodFuncAsync(domainEntity, this.UnitOfWork);

                return await this.HandleDomainMethodResultSaveChangesAndMapAsync(result, domainMethodResult);
            }
            catch (Exception ex)
            {
                return (OperationResult<TOutputDTO>)await this.HandleExceptionResultAsync(result, ex, $"Error running domain method on {EntityTypeName}");
            }
        }

        public async Task<OperationResult<TOutputDTO>> LookupEntityThenCallDomainMethodSaveAndMapAsync<TDomainEntityResult, TOutputDTO>(
            int id,
            Func<TDomainEntity, TUnitOfWork, Task<OperationResult<TDomainEntityResult>>> domainMethodFuncAsync,
            Func<TRepository, int, Task<TDomainEntity>> lookupFuncAsync = null)
            where TDomainEntityResult : class, IIdentity
            where TOutputDTO : class
        {
            var result = new OperationResult<TOutputDTO>();

            try
            {
                var entity = await this.ValidateEntityExistenceAsync(id, result, lookupFuncAsync);

                if (!result.IsSuccessful)
                {
                    return result;
                }

                var domainMethodResult = await domainMethodFuncAsync(entity, this.UnitOfWork);

                return await this.HandleDomainMethodResultSaveChangesAndMapAsync(result, domainMethodResult);
            }
            catch (Exception ex)
            {
                return (OperationResult<TOutputDTO>)await this.HandleExceptionResultAsync(result, ex, $"Error running domain method on {EntityTypeName}");
            }
        }

        public async Task<OperationResult<List<TOutputDTO>>> CallDomainMethodOnGivenEntitySaveAndMapAsync<TDomainEntityResult, TOutputDTO>(
            TDomainEntity domainEntity,
            Func<TDomainEntity, TUnitOfWork, Task<OperationResult<List<TDomainEntityResult>>>> domainMethodFuncAsync)
            where TDomainEntityResult : class, IIdentity
            where TOutputDTO : class
        {
            var result = new OperationResult<List<TOutputDTO>>();

            try
            {
                var domainMethodResult = await domainMethodFuncAsync(domainEntity, this.UnitOfWork);

                // if we're within a transaction, keep a list of all method results obtained so far
                // if the transaction fails, we can iterate through them, calling their OnContextCommitErrorRollbackAction
                this.UnitOfWork.Transaction?.AddResult(domainMethodResult);

                if (!domainMethodResult.IsSuccessful)
                {
                    result.AddErrors(domainMethodResult);
                    await GenericBusinessLogic.TryRollbackResultAsync(domainMethodResult, result);
                    return result;
                }

                // assuming for now that anything which returns a list is just to return a list of dependent child collection items
                // in future, if we need to modify multiple dependent child items and THEN return a list, we need to handle repository actions too
                if (domainMethodResult.Data == null)
                {
                    throw new Exception("No data was returned on the operation result - cannot map, changes NOT committed");
                }

                // SW 2023.08.16 - this was originally added when the domain adapter layer was in use to perform additional checks, don't think it is needed any more
                // await this.Repository.UpdateAsync(domainEntity);
                await this.SaveChangesAsync();

                result.Data = this.Mapper.Map<List<TOutputDTO>>(domainMethodResult.Data);

                return result;
            }
            catch (Exception ex)
            {
                return (OperationResult<List<TOutputDTO>>)await this.HandleExceptionResultAsync(result, ex, $"Error running domain method on {EntityTypeName}");
            }
        }

        public async Task<OperationResult<List<TOutputDTO>>> LookupEntityThenCallDomainMethodSaveAndMapAsync<TDomainEntityResult, TOutputDTO>(
            int id,
            Func<TDomainEntity, TUnitOfWork, Task<OperationResult<List<TDomainEntityResult>>>> domainMethodFuncAsync,
            Func<TRepository, int, Task<TDomainEntity>> lookupFuncAsync = null)
            where TDomainEntityResult : class, IIdentity
            where TOutputDTO : class
        {
            var result = new OperationResult<List<TOutputDTO>>();

            try
            {
                var entity = await this.ValidateEntityExistenceAsync(id, result, lookupFuncAsync);

                if (!result.IsSuccessful)
                {
                    return result;
                }

                return await this.CallDomainMethodOnGivenEntitySaveAndMapAsync<TDomainEntityResult, TOutputDTO>(
                    entity,
                    domainMethodFuncAsync);
            }
            catch (Exception ex)
            {
                return (OperationResult<List<TOutputDTO>>)await this.HandleExceptionResultAsync(result, ex, $"Error running domain method on {EntityTypeName}");
            }
        }

        public async Task<OperationResult> DeleteAsync(int id)
        {
            var result = new OperationResult();

            try
            {
                await this.Repository.DeleteByIdAsync(id);
                await this.SaveChangesAsync();
                return result;
            }
            catch (Exception ex)
            {
                return await this.HandleExceptionResultAsync(result, ex, $"Error deleting {EntityTypeName}");
            }
        }

        public async Task<OperationResult> TruncateAsync()
        {
            var result = new OperationResult();

            try
            {
                await this.Repository.TruncateAsync();
                await this.SaveChangesAsync();
                return result;
            }
            catch(Exception ex)
            {
                return await this.HandleExceptionResultAsync(result, ex, $"Error deleting {EntityTypeName}");
            }
        }

        public Task<OperationResult> ExecuteWrappedTryCatchLogAsync(
            Func<Task<OperationResult>> funcAsync,
            string errorMessage = "Error running business logic method",
            Func<Exception, Task> onExceptionFuncAsync = null)
        {
            return GenericBusinessLogic.ExecuteWrappedTryCatchLogAsync(
                funcAsync: funcAsync,
                logger: this.Logger,
                httpContextAccessor: this.HttpContextAccessor,
                errorMessage: errorMessage,
                onExceptionFuncAsync: onExceptionFuncAsync);
        }

        public Task<OperationResult<T>> ExecuteWrappedTryCatchLogAsync<T>(
            Func<Task<OperationResult<T>>> funcAsync,
            string errorMessage = "Error running business logic method",
            Func<Exception, Task> onExceptionFuncAsync = null)
        {
            return GenericBusinessLogic.ExecuteWrappedTryCatchLogAsync(
                funcAsync: funcAsync,
                logger: this.Logger,
                httpContextAccessor: this.HttpContextAccessor,
                errorMessage: errorMessage,
                onExceptionFuncAsync: onExceptionFuncAsync);
        }

        public Task<OperationResult> ExecuteWrappedTryCatchLogAndSaveChangesAsync(
            Func<Task<OperationResult>> funcAsync,
            string errorMessage = "Error running business logic method",
            Func<Exception, Task> onExceptionFuncAsync = null)
        {
            return GenericBusinessLogic.ExecuteWrappedTryCatchLogAndSaveChangesAsync(
                funcAsync: funcAsync,
                unitOfWork: this.UnitOfWork,
                logger: this.Logger,
                httpContextAccessor: this.HttpContextAccessor,
                errorMessage: errorMessage,
                onExceptionFuncAsync: onExceptionFuncAsync);
        }

        protected async Task<OperationResult<TDomainEntity>> GetByIdInternalAsync(int id, Func<int, Task<TDomainEntity>> lookupFuncAsync = null)
        {
            var result = new OperationResult<TDomainEntity>();

            try
            {
                TDomainEntity entity;

                if (lookupFuncAsync != null)
                {
                    entity = await lookupFuncAsync(id);
                }
                else
                {
                    entity = await this.Repository.GetByIdAsync(id);
                }

                if (entity == null)
                {
                    result.AddError(DomainEntityBase.GetNonExistentOrUnauthorisedEntityMessage<TDomainEntity>(), OperationResultErrorType.EntityNotFoundOrUnauthorized);
                    return result;
                }

                result.Data = entity;

                return result;
            }
            catch (Exception ex)
            {
                return (OperationResult<TDomainEntity>)await this.HandleExceptionResultAsync(result, ex, $"Error getting {EntityTypeName}");
            }
        }

        protected async Task<OperationResult<List<TOutputDTO>>> GetRepositoryDataListAndMapAsync<TEntity, TOutputDTO>(Func<Task<List<TEntity>>> getRepositoryDataFuncAsync)
            where TOutputDTO : class
        {
            var result = new OperationResult<List<TOutputDTO>>();

            try
            {
                var data = await getRepositoryDataFuncAsync();
                result.Data = this.Mapper.Map<List<TOutputDTO>>(data);
            }
            catch (Exception ex)
            {
                var message = "Error executing repository method";
                return (OperationResult<List<TOutputDTO>>)await this.HandleExceptionResultAsync(result, ex, message);
            }

            return result;
        }

        protected OperationResult<TOutputDTO> HandleGetEntityResultAndMap<TDomainEntityResult, TOutputDTO>(
            OperationResult<TOutputDTO> result,
            TDomainEntityResult entity)
            where TOutputDTO : class
        {
            if (entity == null)
            {
                result.AddError(DomainEntityBase.GetNonExistentOrUnauthorisedEntityMessage<TDomainEntityResult>(), OperationResultErrorType.EntityNotFoundOrUnauthorized);
                return result;
            }

            result.Data = this.Mapper.Map<TOutputDTO>(entity);

            return result;
        }

        protected OperationResult<TOutputDTO> HandleGetEntityResultAndMap<TDomainEntityResult, TOutputDTO>(
            OperationResult<TOutputDTO> result,
            TDomainEntity entity,
            Func<TDomainEntity, TDomainEntityResult> entityPropertySelectorFunc)
            where TOutputDTO : class
        {
            if (entity == null)
            {
                result.AddError(DomainEntityBase.GetNonExistentOrUnauthorisedEntityMessage<TDomainEntity>(), OperationResultErrorType.EntityNotFoundOrUnauthorized);
                return result;
            }

            TDomainEntityResult entityProperty = entityPropertySelectorFunc(entity);

            result.Data = this.Mapper.Map<TOutputDTO>(entityProperty);

            return result;
        }

        protected async Task SaveChangesAsync()
        {
            await this.UnitOfWork.SaveChangesAsync();
        }

        private static OperationResult<TDomainEntityResult> HandleDomainMethodResult<TDomainEntityResult>(
            OperationResult<TDomainEntityResult> result,
            OperationResult<TDomainEntityResult> domainMethodResult)
            where TDomainEntityResult : class
        {
            if (!domainMethodResult.IsSuccessful)
            {
                result.AddErrors(domainMethodResult);
                return result;
            }

            result.Data = domainMethodResult.Data;

            return result;
        }

        private async Task<TDomainEntity> ValidateEntityExistenceAsync(
            int id,
            IOperationResult result,
            Func<TRepository, int, Task<TDomainEntity>> lookupFuncAsync)
        {
            TDomainEntity entity;

            if (lookupFuncAsync == null)
            {
                entity = await this.Repository.GetByIdAsync(id);
            }
            else
            {
                entity = await lookupFuncAsync(this.Repository, id);
            }

            if (entity == null)
            {
                result.AddError(DomainEntityBase.GetNonExistentOrUnauthorisedEntityMessage<TDomainEntity>(), OperationResultErrorType.EntityNotFoundOrUnauthorized);
            }

            return entity;
        }

        private async Task<List<TDomainEntity>> ValidateMultipleEntityExistenceAsync(
            IEnumerable<int> ids,
            IOperationResult result,
            Func<TRepository, IEnumerable<int>, Task<List<TDomainEntity>>> lookupFuncAsync)
        {
            var entities = await lookupFuncAsync(this.Repository, ids) ?? throw new InvalidOperationException("Entity list function returned null");

            var missingIds = ids.Except(entities.Select(x => x.Id ) ).ToList();

            if (missingIds.Any())
            {
                result.AddError($"{(missingIds.Count == 1 ? typeof(TDomainEntity).Name : Pluralizer.Pluralize(typeof(TDomainEntity).Name))} with Id{(missingIds.Count == 1 ? string.Empty : "s")} {missingIds.StringJoin(",")} {(missingIds.Count == 1 ? "was" : "were")} not found", OperationResultErrorType.EntityNotFoundOrUnauthorized);
            }

            return entities;
        }

        private async Task<OperationResult> HandleDomainMethodResultAsync(
            OperationResult result,
            OperationResult domainMethodResult)
        {
            // if we're within a transaction, keep a list of all method results obtained so far
            // if the transaction fails, we can iterate through them, calling their OnContextCommitErrorRollbackAction
            this.UnitOfWork.Transaction?.AddResult(domainMethodResult);

            if (!domainMethodResult.IsSuccessful)
            {
                result.AddErrors(domainMethodResult);
                await GenericBusinessLogic.TryRollbackResultAsync(domainMethodResult, result);
                return result;
            }

            return result;
        }

        private async Task<OperationResult<TDomainEntityResult>> HandleTypedDomainMethodResultAsync<TDomainEntityResult>(
            OperationResult<TDomainEntityResult> result,
            OperationResult<TDomainEntityResult> domainMethodResult)
        {
            // if we're within a transaction, keep a list of all method results obtained so far
            // if the transaction fails, we can iterate through them, calling their OnContextCommitErrorRollbackAction
            this.UnitOfWork.Transaction?.AddResult(domainMethodResult);

            if (!domainMethodResult.IsSuccessful)
            {
                result.AddErrors(domainMethodResult);
                await GenericBusinessLogic.TryRollbackResultAsync(domainMethodResult, result);
                return result;
            }

            result.Data = domainMethodResult.Data;

            return result;
        }

        private async Task<OperationResult> HandleDomainMethodResultAndSaveChangesAsync(
            OperationResult result,
            OperationResult domainMethodResult)
        {
            // if we're within a transaction, keep a list of all method results obtained so far
            // if the transaction fails, we can iterate through them, calling their OnContextCommitErrorRollbackAction
            this.UnitOfWork.Transaction?.AddResult(domainMethodResult);

            if (!domainMethodResult.IsSuccessful)
            {
                result.AddErrors(domainMethodResult);
                await GenericBusinessLogic.TryRollbackResultAsync(domainMethodResult, result);
                return result;
            }

            // SW 2023.08.16 - this was originally added when the domain adapter layer was in use to perform additional checks, don't think it is needed any more
            // await this.Repository.UpdateAsync(domainEntity);
            await this.SaveChangesAsync();

            return result;
        }

        private async Task<OperationResult<TDomainEntityResult>> HandleDomainMethodResultAndSaveChangesAsync<TDomainEntityResult>(
            OperationResult<TDomainEntityResult> result,
            OperationResult<TDomainEntityResult> domainMethodResult)
            where TDomainEntityResult : class
        {
            return await GenericBusinessLogic.HandleDomainMethodResultAndSaveChangesAsync<TDomainEntity, TDomainEntityResult, TUnitOfWork>(
                result,
                domainMethodResult,
                this.UnitOfWork);
        }

        private OperationResult<TOutputDTO> HandleDomainMethodResultAndMap<TDomainEntityResult, TOutputDTO>(
            OperationResult<TOutputDTO> result,
            OperationResult<TDomainEntityResult> domainMethodResult)
            where TDomainEntityResult : class
            where TOutputDTO : class
        {
            if (!domainMethodResult.IsSuccessful)
            {
                result.AddErrors(domainMethodResult);
                return result;
            }

            if (domainMethodResult.Data == null)
            {
                throw new Exception("No data was returned on the operation result - cannot map");
            }

            result.Data = this.Mapper.Map<TOutputDTO>(domainMethodResult.Data);

            return result;
        }

        private async Task<OperationResult<TOutputDTO>> HandleDomainMethodResultSaveChangesAndMapAsync<TDomainEntityResult, TOutputDTO>(
            OperationResult<TOutputDTO> result,
            OperationResult<TDomainEntityResult> domainMethodResult)
            where TDomainEntityResult : class
            where TOutputDTO : class
        {
            return await GenericBusinessLogic.HandleDomainMethodResultSaveChangesAndMapAsync<TDomainEntity, TDomainEntityResult, TOutputDTO, TUnitOfWork>(
                result,
                domainMethodResult,
                this.UnitOfWork,
                this.Mapper);
        }

        private async Task<OperationResult> HandleExceptionResultAsync(OperationResult result, Exception ex, string message)
        {
            return await GenericBusinessLogic.HandleExceptionResultAsync(
                result: result,
                ex: ex,
                message: message,
                logger: this.Logger,
                httpContextAccessor: this.HttpContextAccessor);
        }
    }

    /// <summary>
    /// Class for static implementation of helper methods.
    /// </summary>
    public sealed class GenericBusinessLogic
    {
        public static async Task<OperationResult<T>> ExecuteWrappedTryCatchLogAsync<T>(
            Func<Task<OperationResult<T>>> funcAsync,
            ILogger logger,
            string errorMessage = "Error running business logic method",
            Func<Exception, Task> onExceptionFuncAsync = null)
        {
            return await ExecuteWrappedTryCatchLogAsync(
                funcAsync: funcAsync,
                logger: logger,
                httpContextAccessor: null,
                errorMessage: errorMessage,
                onExceptionFuncAsync: onExceptionFuncAsync);
        }

        public static async Task<OperationResult<T>> ExecuteWrappedTryCatchLogAsync<T>(
            Func<Task<OperationResult<T>>> funcAsync,
            ILogger logger,
            IHttpContextAccessor httpContextAccessor,
            string errorMessage = "Error running business logic method",
            Func<Exception, Task> onExceptionFuncAsync = null)
        {
            var result = new OperationResult<T>();

            try
            {
                result = await funcAsync();

                if (!result.IsSuccessful)
                {
                    await TryRollbackResultAsync(result, result);
                }

                return result;
            }
            catch (Exception ex)
            {
                if (onExceptionFuncAsync != null)
                {
                    try
                    {
                        await onExceptionFuncAsync(ex);
                    }
                    catch (Exception onExceptionFuncEx)
                    {
                        logger.LogError(onExceptionFuncEx, $"Error running {nameof(onExceptionFuncAsync)}");
                    }
                }

                return (OperationResult<T>)await HandleExceptionResultAsync(
                    result,
                    ex,
                    errorMessage,
                    logger,
                    httpContextAccessor);
            }
        }

        public static async Task<OperationResult> ExecuteWrappedTryCatchLogAndSaveChangesAsync<TUnitOfWork>(
            Func<Task<OperationResult>> funcAsync,
            TUnitOfWork unitOfWork,
            ILogger logger,
            string errorMessage = "Error running business logic method",
            Func<Exception, Task> onExceptionFuncAsync = null)
            where TUnitOfWork : class, IUnitOfWork
        {
            return await ExecuteWrappedTryCatchLogAndSaveChangesAsync(
                funcAsync: funcAsync,
                unitOfWork: unitOfWork,
                logger: logger,
                httpContextAccessor: null,
                errorMessage: errorMessage,
                onExceptionFuncAsync: onExceptionFuncAsync);
        }

        public static async Task<OperationResult> ExecuteWrappedTryCatchLogAndSaveChangesAsync<TUnitOfWork>(
            Func<Task<OperationResult>> funcAsync,
            TUnitOfWork unitOfWork,
            ILogger logger,
            IHttpContextAccessor httpContextAccessor,
            string errorMessage = "Error running business logic method",
            Func<Exception, Task> onExceptionFuncAsync = null)
            where TUnitOfWork : class, IUnitOfWork
        {
            var result = new OperationResult();

            try
            {
                result = await funcAsync();

                if (!result.IsSuccessful)
                {
                    await TryRollbackResultAsync(result, result);
                }

                await SaveChangesAsync(unitOfWork);

                return result;
            }
            catch (Exception ex)
            {
                if (onExceptionFuncAsync != null)
                {
                    try
                    {
                        await onExceptionFuncAsync(ex);
                    }
                    catch (Exception onExceptionFuncEx)
                    {
                        logger.LogError(onExceptionFuncEx, $"Error running {nameof(onExceptionFuncAsync)}");
                    }
                }

                return await HandleExceptionResultAsync(
                    result,
                    ex,
                    errorMessage,
                    logger,
                    httpContextAccessor);
            }
        }

        public static async Task<OperationResult<T>> ExecuteWrappedTryCatchLogAndSaveChangesAsync<T, TUnitOfWork>(
            Func<Task<OperationResult<T>>> funcAsync,
            TUnitOfWork unitOfWork,
            ILogger logger,
            IHttpContextAccessor httpContextAccessor,
            string errorMessage = "Error running business logic method",
            Func<Exception, Task> onExceptionFuncAsync = null)
            where TUnitOfWork : class, IUnitOfWork
        {
            var result = new OperationResult<T>();

            try
            {
                result = await funcAsync();

                if (!result.IsSuccessful)
                {
                    await TryRollbackResultAsync(result, result);
                }

                await SaveChangesAsync(unitOfWork);

                return result;
            }
            catch (Exception ex)
            {
                if (onExceptionFuncAsync != null)
                {
                    try
                    {
                        await onExceptionFuncAsync(ex);
                    }
                    catch (Exception onExceptionFuncEx)
                    {
                        logger.LogError(onExceptionFuncEx, $"Error running {nameof(onExceptionFuncAsync)}");
                    }
                }

                return (OperationResult<T>)await HandleExceptionResultAsync(
                    result,
                    ex,
                    errorMessage,
                    logger,
                    httpContextAccessor);
            }
        }

        public static async Task<OperationResult> ExecuteWrappedTryCatchLogAsync(
            Func<Task<OperationResult>> funcAsync,
            ILogger logger,
            string errorMessage = "Error running business logic method",
            Func<Exception, Task> onExceptionFuncAsync = null)
        {
            return await ExecuteWrappedTryCatchLogAsync(
                funcAsync: funcAsync,
                logger: logger,
                errorMessage: errorMessage,
                httpContextAccessor: null,
                onExceptionFuncAsync: onExceptionFuncAsync);
        }

        public static async Task<OperationResult> ExecuteWrappedTryCatchLogAsync(
            Func<Task<OperationResult>> funcAsync,
            ILogger logger,
            IHttpContextAccessor httpContextAccessor,
            string errorMessage = "Error running business logic method",
            Func<Exception, Task> onExceptionFuncAsync = null)
        {
            var result = new OperationResult();

            try
            {
                result = await funcAsync();

                if (!result.IsSuccessful)
                {
                    await TryRollbackResultAsync(result, result);
                }

                return result;
            }
            catch (Exception ex)
            {
                if (onExceptionFuncAsync != null)
                {
                    try
                    {
                        await onExceptionFuncAsync(ex);
                    }
                    catch (Exception onExceptionFuncEx)
                    {
                        logger.LogError(onExceptionFuncEx, $"Error running {nameof(onExceptionFuncAsync)}");
                    }
                }

                return await HandleExceptionResultAsync(
                    result: result,
                    ex: ex,
                    message: errorMessage,
                    logger: logger,
                    httpContextAccessor: httpContextAccessor);
            }
        }

        public static async Task<OperationResult<TDomainEntityResult>> CallDomainMethodOnGivenEntityAndSaveAsync<TDomainEntity, TDomainEntityResult, TUnitOfWork>(
            TDomainEntity domainEntity,
            Func<TDomainEntity, TUnitOfWork, Task<OperationResult<TDomainEntityResult>>> domainMethodFuncAsync,
            ILogger logger,
            TUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor)
            where TDomainEntity : class, IIdentity
            where TDomainEntityResult : class
            where TUnitOfWork : IUnitOfWork
        {
            var result = new OperationResult<TDomainEntityResult>();

            try
            {
                var domainMethodResult = await domainMethodFuncAsync(domainEntity, unitOfWork);

                return await HandleDomainMethodResultAndSaveChangesAsync<TDomainEntity, TDomainEntityResult, TUnitOfWork>(
                    result,
                    domainMethodResult,
                    unitOfWork);
            }
            catch (Exception ex)
            {
                return (OperationResult<TDomainEntityResult>)await HandleExceptionResultAsync(
                    result: result,
                    ex: ex,
                    message: $"Error running domain method on {domainEntity.GetType().Name}",
                    logger: logger,
                    httpContextAccessor: httpContextAccessor);
            }
        }

        internal static async Task<OperationResult<TDomainEntityResult>> HandleDomainMethodResultAndSaveChangesAsync<TDomainEntity, TDomainEntityResult, TUnitOfWork>(
            OperationResult<TDomainEntityResult> overallResult,
            OperationResult<TDomainEntityResult> domainMethodResult,
            TUnitOfWork unitOfWork)
            where TDomainEntity : IIdentity
            where TDomainEntityResult : class
            where TUnitOfWork : IUnitOfWork
        {
            // if we're within a transaction, keep a list of all method results obtained so far
            // if the transaction fails, we can iterate through them, calling their OnContextCommitErrorRollbackAction
            unitOfWork.Transaction?.AddResult(domainMethodResult);

            if (!domainMethodResult.IsSuccessful)
            {
                overallResult.AddErrors(domainMethodResult);
                await TryRollbackResultAsync(domainMethodResult, overallResult);
                return overallResult;
            }

            // SW 2023.08.16 - this was originally added when the domain adapter layer was in use to perform additional checks, don't think it is needed any more
            // var repository = unitOfWork.GetRepository<TDomainEntity>();
            // await repository.UpdateAsync(domainEntity);
            await SaveChangesAsync(unitOfWork);

            overallResult.Data = domainMethodResult.Data;

            return overallResult;
        }

        internal static async Task<OperationResult<TOutputDTO>> HandleDomainMethodResultSaveChangesAndMapAsync<TDomainEntity, TDomainEntityResult, TOutputDTO, TUnitOfWork>(
            OperationResult<TOutputDTO> overallResult,
            OperationResult<TDomainEntityResult> domainMethodResult,
            TUnitOfWork unitOfWork,
            IMapper mapper)
            where TDomainEntity : IIdentity
            where TDomainEntityResult : class
            where TOutputDTO : class
            where TUnitOfWork : IUnitOfWork
        {
            // if we're within a transaction, keep a list of all method results obtained so far
            // if the transaction fails, we can iterate through them, calling their OnContextCommitErrorRollbackAction
            unitOfWork.Transaction?.AddResult(domainMethodResult);

            if (!domainMethodResult.IsSuccessful)
            {
                overallResult.AddErrors(domainMethodResult);
                await TryRollbackResultAsync(domainMethodResult, overallResult);
                return overallResult;
            }

            if (domainMethodResult.Data == null)
            {
                throw new Exception("No data was returned on the operation result - cannot map, changes NOT committed");
            }

            // SW 2023.08.16 - this was originally added when the domain adapter layer was in use to perform additional checks, don't think it is needed any more
            // var repository = unitOfWork.GetRepository<TDomainEntity>();
            // await repository.UpdateAsync(domainEntity);
            await SaveChangesAsync(unitOfWork);

            overallResult.Data = mapper.Map<TOutputDTO>(domainMethodResult.Data);

            return overallResult;
        }

        internal static async Task<OperationResult> HandleExceptionResultAsync(
            OperationResult result,
            Exception ex,
            string message,
            ILogger logger,
            IHttpContextAccessor httpContextAccessor)
        {
            var exceptions = new List<Exception> { ex };

            var logMessage = message;

            if (ex is not ConsumerFriendlyException && httpContextAccessor?.HttpContext != null)
            {
                var additionalInfo = Environment.NewLine
                    + "  HTTP REQUEST DETAILS"
                    + Environment.NewLine
                    + "  --------------------"
                    + Environment.NewLine
                    + await LoggingUtility.HttpRequestToLogStringAsync(httpContextAccessor.HttpContext);

                using (LogContext.PushProperty(LoggingUtility.SerilogAdditionalLogInfoContextProperty, additionalInfo))
                {
                    logger.LogError(ex, logMessage);
                }
            }
            else
            {
                logger.LogError(ex, logMessage);
            }

            if (EnvironmentUtility.IsInUnitTestMode)
            {
                // result.DoNotAddStackTraceToErrorsInUnitTestMode = true;
                // result.AddError(ex.ToString());
            }

            message = GetOperationResultErrorMessage(ex, message);

            result.DoNotAddStackTraceToErrorsInUnitTestMode = true;
            result.AddError(message);

            try
            {
                await result.RollbackAsync(result);
            }
            catch (Exception ex1)
            {
                exceptions.Add(new Exception("Error rolling back operation result", ex1));
            }

            // no rollback exception occurred, and the exception is a DbUpdateException
            // allow this to go back to the client, for now - it is useful for tracking down referential integrity violations we need to fix
            if (exceptions.Count == 1 && exceptions[0] is DbUpdateException)
            {
                result.AddError(exceptions[0].Message, OperationResultErrorType.Critical);
                return result;
            }
            else if (exceptions.Count > 1)
            {
                // ex = new AggregateException("An unhandled exception occurred, and additionally one or more exceptions occurred trying to rollback operations", exceptions);
            }

            // if we're debugging, we want a chance to capture the thrown exception at the
            // global exception handler in the presentation layer
            if (Debugger.IsAttached)
            {
                // preserve the original stack trace
                // ExceptionDispatchInfo.Capture(ex).Throw();
            }

            return result;
        }

        internal static async Task TryRollbackResultAsync(OperationResult resultToRollback, OperationResult resultToAddRollbackErrorsTo)
        {
            try
            {
                await resultToRollback.RollbackAsync(resultToAddRollbackErrorsTo);
            }
            catch (Exception ex)
            {
                throw new Exception("Error rolling back operation result", ex);
            }
        }

        internal static async Task SaveChangesAsync<TUnitOfWork>(TUnitOfWork unitOfWork)
            where TUnitOfWork : IUnitOfWork
        {
            await unitOfWork.SaveChangesAsync();
        }

        internal static string GetOperationResultErrorMessage(Exception ex, string defaultMessage)
        {
            if (ex is ConsumerFriendlyException consumerFriendlyException && !string.IsNullOrWhiteSpace(consumerFriendlyException.Message))
            {
                return consumerFriendlyException.Message;
            }

            return defaultMessage;
        }
    }
}
