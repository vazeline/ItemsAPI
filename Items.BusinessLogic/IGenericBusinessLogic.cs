using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Models;
using Items.Data.EFCore.Abstraction.Interfaces;
using Items.Data.EFCore.Entities.Interfaces;

namespace Items.GenericServices.Interfaces
{
    public interface IGenericBusinessLogic<TDomainEntity, TUnitOfWork, out TRepository>
        where TDomainEntity : class, IIdentity
        where TUnitOfWork : class, IUnitOfWork
        where TRepository : class, IBaseRepository<TDomainEntity>
    {
        TRepository Repository { get; }

        /// <summary>
        /// Deletes a domain entity by its Id.
        /// </summary>
        /// <param name="id">The Id of the domain entity to be deleted.</param>
        /// <returns>OperationResult indicating success or failure.</returns>
        Task<OperationResult> DeleteAsync(int id);

        /// <summary>
        /// Get a domain entity by its Id, and return the domain entity.
        /// </summary>
        /// <param name="id">The Id of the domain entity to be retrieved.</param>
        /// <returns>The retrieved domain entity.</returns>
        Task<OperationResult<TDomainEntity>> GetByIdAsync(int id);

        /// <summary>
        /// Get a domain entity by its Id using a custom lookup func, and return the domain entity.
        /// </summary>
        /// <param name="id">The Id of the domain entity to be retrieved.</param>
        /// <param name="lookupFuncAsync">A lookup func to override the default GetById method.</param>
        /// <returns>The retrieved domain entity.</returns>
        Task<OperationResult<TDomainEntity>> GetByIdAsync(int id, Func<TRepository, int, Task<TDomainEntity>> lookupFuncAsync);

        /// <summary>
        /// Get a domain entity by its Id, and return the domain entity mapped to an output DTO.
        /// </summary>
        /// <typeparam name="TOutputDTO">The type of the output DTO, to which the found entity will be mapped.</typeparam>
        /// <param name="id">The Id of the domain entity to be retrieved.</param>
        /// <param name="lookupFuncAsync">An optional lookup func to override the default GetById method.</param>
        /// <returns>The mapped output DTO.</returns>
        Task<OperationResult<TOutputDTO>> GetByIdAndMapAsync<TOutputDTO>(int id, Func<TRepository, int, Task<TDomainEntity>> lookupFuncAsync = null)
            where TOutputDTO : class;

        /// <summary>
        /// Get a domain entity by a repository method which returns a single instance of the domain entity, and return the domain entity mapped to an output DTO.
        /// </summary>
        /// <typeparam name="TOutputDTO">The type of the output DTO, to which the found entity will be mapped.</typeparam>
        /// <param name="lookupFuncAsync">An optional lookup func to override the default GetById method.</param>
        /// <returns>The mapped output DTO.</returns>
        Task<OperationResult<TOutputDTO>> GetAndMapAsync<TOutputDTO>(Func<TRepository, Task<TDomainEntity>> lookupFuncAsync)
            where TOutputDTO : class;

        /// <summary>
        /// Get a domain entity by its Id, and return the domain entity mapped to an output DTO, where the return type differs from the type of the domain entity repository on which the lookpu func is called.
        /// </summary>
        /// <typeparam name="TDomainEntityResult">The type of the domain entity which will be returned from the repository function.</typeparam>
        /// <typeparam name="TOutputDTO">The type of the output DTO, to which the found entity will be mapped.</typeparam>
        /// <param name="id">The Id of the domain entity to be retrieved.</param>
        /// <param name="lookupFuncAsync">An optional lookup func to override the default GetById method.</param>
        /// <returns>The mapped output DTO.</returns>
        Task<OperationResult<TOutputDTO>> GetByIdAndMapAsync<TDomainEntityResult, TOutputDTO>(int id, Func<TRepository, int, Task<TDomainEntityResult>> lookupFuncAsync = null)
            where TDomainEntityResult : class
            where TOutputDTO : class;

        /// <summary>
        /// Get a domain entity by its Id, and if found, return a property of the domain entity mapped to an output DTO.
        /// </summary>
        /// <typeparam name="TDomainEntityResult">The type of the domain entity property which will be returned from the property selector function.</typeparam>
        /// <typeparam name="TOutputDTO">The type of the output DTO, to which the found entity will be mapped.</typeparam>
        /// <param name="id">The Id of the domain entity to be retrieved.</param>
        /// <param name="entityPropertySelectorFunc">A func to select a property of the domain entity.</param>
        /// <param name="lookupFuncAsync">An optional lookup func to override the default GetById method.</param>
        /// <returns>The mapped output DTO.</returns>
        Task<OperationResult<TOutputDTO>> GetByIdAndMapAsync<TDomainEntityResult, TOutputDTO>(
            int id,
            Func<TDomainEntity, TDomainEntityResult> entityPropertySelectorFunc,
            Func<TRepository, int, Task<TDomainEntity>> lookupFuncAsync = null)
            where TDomainEntityResult : class
            where TOutputDTO : class;

        /// <summary>
        /// Get a pre-mapped output DTO for when we need to retrieve the result of a summary/groupby operation directly from the repository.
        /// </summary>
        /// <typeparam name="TOutputDTO">The type of the output DTO.</typeparam>
        /// <param name="lookupFuncAsync">Repository summary method.</param>
        /// <returns>The output DTO.</returns>
        Task<OperationResult<TOutputDTO>> GetForSummaryOperationsAsync<TOutputDTO>(
            Func<TRepository, Task<TOutputDTO>> lookupFuncAsync)
            where TOutputDTO : class;

        /// <summary>
        /// List domain entities, and return a list of the domain entities mapped to an output DTO.
        /// </summary>
        /// <typeparam name="TOutputDTO">The type of the output DTO, to which the listed entities will be mapped.</typeparam>
        /// <param name="listFuncAsync">An optional list function to return a specific list.</param>
        /// <returns>A list of the mapped output DTOs.</returns>
        Task<OperationResult<List<TOutputDTO>>> ListAndMapAsync<TOutputDTO>(Func<TRepository, Task<List<TDomainEntity>>> listFuncAsync = null)
            where TOutputDTO : class;

        /// <summary>
        /// List domain entities of a different type, and return a list of the domain entities mapped to an output DTO.
        /// </summary>
        /// <typeparam name="TDomainEntityResult">The type of the domain entities which will be listed.</typeparam>
        /// <typeparam name="TOutputDTO">The type of the output DTO, to which the listed entities will be mapped.</typeparam>
        /// <param name="listFuncAsync">An optional list function to return a specific list.</param>
        /// <returns>A list of the mapped output DTOs.</returns>
        Task<OperationResult<List<TOutputDTO>>> ListAndMapAsync<TDomainEntityResult, TOutputDTO>(Func<TRepository, Task<List<TDomainEntityResult>>> listFuncAsync)
            where TDomainEntityResult : class
            where TOutputDTO : class;

        /// <summary>
        /// Call the specified domain entity creation func, add the domain entity to the context, and return the domain entity.
        /// </summary>
        /// <param name="entityCreationFuncAsync">The entity creation func.</param>
        /// <returns>The created domain entity.</returns>
        Task<OperationResult<TDomainEntity>> CreateSingleEntityAndSaveAsync(Func<TUnitOfWork, Task<OperationResult<TDomainEntity>>> entityCreationFuncAsync);

        /// <summary>
        /// Call the specified domain entity creation func, add the domain entity to the context, and return the id of the new domain entity.
        /// </summary>
        /// <param name="entityCreationFuncAsync">The entity creation func.</param>
        /// <returns>The created domain entity.</returns>
        Task<OperationResult<int>> CreateSingleEntitySaveAndReturnIdAsync(Func<TUnitOfWork, Task<OperationResult<TDomainEntity>>> entityCreationFuncAsync);

        /// <summary>
        /// Call the specified domain entity creation func which returns a list, add the domain entities to the context, and return the domain entities.
        /// </summary>
        /// <param name="entityCreationFuncAsync">The entity creation func.</param>
        /// <returns>The created domain entities.</returns>
        Task<OperationResult<List<TDomainEntity>>> CreateEntityListAndSaveAsync(Func<TUnitOfWork, Task<OperationResult<List<TDomainEntity>>>> entityCreationFuncAsync);

        /// <summary>
        /// Call the specified domain entity creation func, add the domain entity to the context, and return the domain entity mapped to an output DTO.
        /// </summary>
        /// <typeparam name="TOutputDTO">The type of the output DTO, to which the created entity will be mapped.</typeparam>
        /// <param name="entityCreationFuncAsync">The entity creation func.</param>
        /// <returns>The mapped output DTO.</returns>
        Task<OperationResult<TOutputDTO>> CreateSingleEntitySaveAndMapAsync<TOutputDTO>(Func<TUnitOfWork, Task<OperationResult<TDomainEntity>>> entityCreationFuncAsync)
            where TOutputDTO : class;

        /// <summary>
        /// Call the specified func <b>on the given domain entity</b>.
        /// </summary>
        /// <typeparam name="TDomainEntityResult">The return type of the domain entity func.</typeparam>
        /// <param name="domainEntity">The existing domain entity.</param>
        /// <param name="domainMethodFuncAsync">The domain entity func to call.</param>
        /// <returns>An OperationResult indicating success or failure.</returns>
        Task<OperationResult<TDomainEntityResult>> CallDomainMethodOnGivenEntityAsync<TDomainEntityResult>(
            TDomainEntity domainEntity,
            Func<TDomainEntity, TUnitOfWork, Task<OperationResult<TDomainEntityResult>>> domainMethodFuncAsync)
            where TDomainEntityResult : class;

        /// <summary>
        /// <b>Lookup a domain entity by id</b>, then call the specified domain entity func where the result is a list, and map the result to the specified output DTO type.
        /// </summary>
        /// <typeparam name="TDomainEntityResult">The return type of the domain entity func.</typeparam>
        /// <typeparam name="TOutputDTO">The type of the output DTO, to which the created entity will be mapped.</typeparam>
        /// <param name="id">The Id of the domain entity on which to call the domain entity func.</param>
        /// <param name="domainMethodFuncAsync">The domain entity func to call.</param>
        /// <param name="lookupFuncAsync">An optional repository lookup func, for including different child relationships.</param>
        /// <returns>An OperationResult indicating success or failure.</returns>
        Task<OperationResult<List<TOutputDTO>>> LookupEntityThenCallDomainMethodAndMapAsync<TDomainEntityResult, TOutputDTO>(
            int id,
            Func<TDomainEntity, TUnitOfWork, Task<OperationResult<List<TDomainEntityResult>>>> domainMethodFuncAsync,
            Func<TRepository, int, Task<TDomainEntity>> lookupFuncAsync = null)
            where TDomainEntityResult : class
            where TOutputDTO : class;

        /// <summary>
        /// <b>Lookup a domain entity by id</b>, then call the specified domain entity func, and map the result to the specified output DTO type.
        /// </summary>
        /// <typeparam name="TDomainEntityResult">The return type of the domain entity func.</typeparam>
        /// <typeparam name="TOutputDTO">The type of the output DTO, to which the created entity will be mapped.</typeparam>
        /// <param name="id">The Id of the domain entity on which to call the domain entity func.</param>
        /// <param name="domainMethodFuncAsync">The domain entity func to call.</param>
        /// <param name="lookupFuncAsync">An optional repository lookup func, for including different child relationships.</param>
        /// <returns>An OperationResult indicating success or failure.</returns>
        Task<OperationResult<TOutputDTO>> LookupEntityThenCallDomainMethodAndMapAsync<TDomainEntityResult, TOutputDTO>(
           int id,
           Func<TDomainEntity, TUnitOfWork, Task<OperationResult<TDomainEntityResult>>> domainMethodFuncAsync,
           Func<TRepository, int, Task<TDomainEntity>> lookupFuncAsync = null)
           where TDomainEntityResult : class
           where TOutputDTO : class;

        /// <summary>
        /// Call the specified func <b>on the given domain entity</b> where the result is a list, and map the result to the specified output DTO type.
        /// </summary>
        /// <typeparam name="TDomainEntityResult">The return type of the domain entity func.</typeparam>
        /// <typeparam name="TOutputDTO">The type of the output DTO, to which the created entity will be mapped.</typeparam>
        /// <param name="domainEntity">The existing domain entity.</param>
        /// <param name="domainMethodFuncAsync">The domain entity func to call.</param>
        /// <returns>An OperationResult indicating success or failure.</returns>
        Task<OperationResult<List<TOutputDTO>>> CallDomainMethodOnGivenEntityAndMapAsync<TDomainEntityResult, TOutputDTO>(
            TDomainEntity domainEntity,
            Func<TDomainEntity, TUnitOfWork, Task<OperationResult<List<TDomainEntityResult>>>> domainMethodFuncAsync)
            where TDomainEntityResult : class
            where TOutputDTO : class;

        /// <summary>
        /// Call the specified func <b>on the given domain entity</b>, and commit changes to the context.
        /// </summary>
        /// <typeparam name="TDomainEntityResult">The return type of the domain entity func.</typeparam>
        /// <param name="domainEntity">The existing domain entity.</param>
        /// <param name="domainMethodFuncAsync">The domain entity func to call.</param>
        /// <returns>An OperationResult indicating success or failure.</returns>
        Task<OperationResult<TDomainEntityResult>> CallDomainMethodOnGivenEntityAndSaveAsync<TDomainEntityResult>(
            TDomainEntity domainEntity,
            Func<TDomainEntity, TUnitOfWork, Task<OperationResult<TDomainEntityResult>>> domainMethodFuncAsync)
            where TDomainEntityResult : class;

        /// <summary>
        /// <b>Lookup a domain entity by id</b>, then call the specified domain entity func.
        /// </summary>
        /// <typeparam name="TDomainEntityResult">The return type of the domain entity func.</typeparam>
        /// <param name="id">The Id of the domain entity on which to call the domain entity func.</param>
        /// <param name="domainMethodFuncAsync">The domain entity func to call.</param>
        /// <param name="lookupFuncAsync">An optional repository lookup func, for including different child relationships.</param>
        /// <returns>An OperationResult indicating success or failure.</returns>
        Task<OperationResult<TDomainEntityResult>> LookupEntityThenCallDomainMethodAsync<TDomainEntityResult>(
            int id,
            Func<TDomainEntity, TUnitOfWork, Task<OperationResult<TDomainEntityResult>>> domainMethodFuncAsync,
            Func<TRepository, int, Task<TDomainEntity>> lookupFuncAsync = null);

        /// <summary>
        /// <b>Lookup a domain entity by id</b>, then call the specified domain entity func, and commit the changes to the context.
        /// </summary>
        /// <typeparam name="TDomainEntityResult">The return type of the domain entity func.</typeparam>
        /// <param name="id">The Id of the domain entity on which to call the domain entity func.</param>
        /// <param name="domainMethodFuncAsync">The domain entity func to call.</param>
        /// <param name="lookupFuncAsync">An optional repository lookup func, for including different child relationships.</param>
        /// <returns>An OperationResult indicating success or failure.</returns>
        Task<OperationResult<TDomainEntityResult>> LookupEntityThenCallDomainMethodAndSaveAsync<TDomainEntityResult>(
            int id,
            Func<TDomainEntity, TUnitOfWork, Task<OperationResult<TDomainEntityResult>>> domainMethodFuncAsync,
            Func<TRepository, int, Task<TDomainEntity>> lookupFuncAsync = null)
            where TDomainEntityResult : class;

        /// <summary>
        /// Call the specified domain entity func <b>on the given domain entity</b>, and commits the changes to the context.
        /// </summary>
        /// <param name="domainEntity">The existing domain entity.</param>
        /// <param name="domainMethodFuncAsync">The domain entity func to call.</param>
        /// <returns>An OperationResult indicating success or failure.</returns>
        Task<OperationResult> CallDomainMethodOnGivenEntityAndSaveAsync(
            TDomainEntity domainEntity,
            Func<TDomainEntity, TUnitOfWork, Task<OperationResult>> domainMethodFuncAsync);

        /// <summary>
        /// <b>Lookup a domain entity by id</b>, then call the specified domain entity func, and commit the changes to the context.
        /// </summary>
        /// <param name="id">The Id of the domain entity on which to call the domain entity func.</param>
        /// <param name="domainMethodFuncAsync">The domain entity func to call.</param>
        /// <param name="lookupFuncAsync">An optional repository lookup func, for including different child relationships.</param>
        /// <returns>An OperationResult indicating success or failure.</returns>
        Task<OperationResult> LookupEntityThenCallDomainMethodAndSaveAsync(
            int id,
            Func<TDomainEntity, TUnitOfWork, Task<OperationResult>> domainMethodFuncAsync,
            Func<TRepository, int, Task<TDomainEntity>> lookupFuncAsync = null);

        /// <summary>
        /// <b>Lookup a list of domain entities using a repository func</b>, then call the specified domain entity func on each, and commit the changes to the context if all are successful.
        /// </summary>
        /// <param name="ids">The ids of the entities to look up.</param>
        /// <param name="domainMethodFuncAsync">The domain entity func to call.</param>
        /// <param name="lookupFuncAsync">An optional repository lookup func, for including different child relationships.</param>
        /// <returns>An OperationResult indicating success or failure.</returns>
        Task<OperationResult<List<OperationResult<TDomainEntity>>>> LookupEntityListThenCallDomainMethodOnEachAndSaveAsync(
            IEnumerable<int> ids,
            Func<TDomainEntity, TUnitOfWork, Task<OperationResult<TDomainEntity>>> domainMethodFuncAsync,
            Func<TRepository, IEnumerable<int>, Task<List<TDomainEntity>>> lookupFuncAsync);

        /// <summary>
        /// <b>Lookup a list of domain entities using a repository func</b>, then call the specified domain entity func on each, and commit the changes to the context if all are successful.
        /// </summary>
        /// <param name="ids">The ids of the entities to look up.</param>
        /// <param name="domainMethodFuncAsync">The domain entity func to call.</param>
        /// <param name="lookupFuncAsync">An optional repository lookup func, for including different child relationships.</param>
        /// <returns>An OperationResult indicating success or failure.</returns>
        Task<OperationResult<List<OperationResult>>> LookupEntityListThenCallDomainMethodOnEachAndSaveAsync(
            IEnumerable<int> ids,
            Func<TDomainEntity, TUnitOfWork, Task<OperationResult>> domainMethodFuncAsync,
            Func<TRepository, IEnumerable<int>, Task<List<TDomainEntity>>> lookupFuncAsync);

        /// <summary>
        /// <b>Lookup a domain entity by id</b>, then call the specified domain entity func, commit the changes to the context, and return the result of the domain entity func mapped to an output DTO.
        /// </summary>
        /// <typeparam name="TOutputDTO">The type of the output DTO, to which the result of the domain entity func will be mapped.</typeparam>
        /// <param name="id">The Id of the domain entity on which to call the domain entity func.</param>
        /// <param name="domainMethodFuncAsync">The domain entity func to call.</param>
        /// <param name="lookupFuncAsync">An optional repository lookup func, for including different child relationships.</param>
        /// <returns>The result of the domain entity func mapped to an output DTO.</returns>
        Task<OperationResult<TOutputDTO>> LookupEntityThenCallDomainMethodSaveAndMapAsync<TOutputDTO>(
            int id,
            Func<TDomainEntity, TUnitOfWork, Task<OperationResult<TDomainEntity>>> domainMethodFuncAsync,
            Func<TRepository, int, Task<TDomainEntity>> lookupFuncAsync = null)
            where TOutputDTO : class;

        /// <summary>
        /// Call the specified domain entity func <b>on the given domain entity</b> where the return type of the func differs from the type of the domain entity on which it is called, add the domain entity to the context (it is assumed that if we are returning a different domain entity result type to the type on which the domain method is called, it is because we are creating a child item), and return the result of the domain entity func mapped to an output DTO.
        /// </summary>
        /// <typeparam name="TDomainEntityResult">The type of the result of the domain entity func.</typeparam>
        /// <typeparam name="TOutputDTO">The type of the output DTO, to which the result of the domain entity func will be mapped.</typeparam>
        /// <param name="domainMethodFuncAsync">The domain entity func to call.</param>
        /// <returns>The result of the domain entity func mapped to an output DTO.</returns>
        Task<OperationResult<TOutputDTO>> CallDomainMethodOnGivenEntitySaveAndMapAsync<TDomainEntityResult, TOutputDTO>(
            TDomainEntity domainEntity,
            Func<TDomainEntity, TUnitOfWork, Task<OperationResult<TDomainEntityResult>>> domainMethodFuncAsync)
            where TDomainEntityResult : class, IIdentity
            where TOutputDTO : class;

        /// <summary>
        /// <b>Lookup a domain entity by id</b>, then call the specified domain entity func where the return type of the func differs from the type of the domain entity on which it is called, add the domain entity to the context (it is assumed that if we are returning a different domain entity result type to the type on which the domain method is called, it is because we are creating a child item), and return the result of the domain entity func mapped to an output DTO.
        /// </summary>
        /// <typeparam name="TDomainEntityResult">The type of the result of the domain entity func.</typeparam>
        /// <typeparam name="TOutputDTO">The type of the output DTO, to which the result of the domain entity func will be mapped.</typeparam>
        /// <param name="id">The Id of the domain entity on which to call the domain entity func.</param>
        /// <param name="domainMethodFuncAsync">The domain entity func to call.</param>
        /// <param name="lookupFuncAsync">An optional repository lookup func, for including different child relationships.</param>
        /// <returns>The result of the domain entity func mapped to an output DTO.</returns>
        Task<OperationResult<TOutputDTO>> LookupEntityThenCallDomainMethodSaveAndMapAsync<TDomainEntityResult, TOutputDTO>(
            int id,
            Func<TDomainEntity, TUnitOfWork, Task<OperationResult<TDomainEntityResult>>> domainMethodFuncAsync,
            Func<TRepository, int, Task<TDomainEntity>> lookupFuncAsync = null)
            where TDomainEntityResult : class, IIdentity
            where TOutputDTO : class;

        /// <summary>
        /// Call the specified domain entity func <b>on the given domain entity</b>, where the return type of the func differs from the type of the domain entity on which it is called and returns a list, and return the result of the domain entity func mapped to a list of output DTOs.
        /// </summary>
        /// <typeparam name="TDomainEntityResult">The type of the result of the domain entity func.</typeparam>
        /// <typeparam name="TOutputDTO">The type of the output DTO, to which the result of the domain entity func will be mapped.</typeparam>
        /// <param name="domainMethodFuncAsync">The domain entity func to call.</param>
        /// <returns>The result of the domain entity func mapped to an output DTO.</returns>
        Task<OperationResult<List<TOutputDTO>>> CallDomainMethodOnGivenEntitySaveAndMapAsync<TDomainEntityResult, TOutputDTO>(
            TDomainEntity domainEntity,
            Func<TDomainEntity, TUnitOfWork, Task<OperationResult<List<TDomainEntityResult>>>> domainMethodFuncAsync)
            where TDomainEntityResult : class, IIdentity
            where TOutputDTO : class;

        /// <summary>
        /// <b>Lookup a domain entity by id</b>, then call the specified domain entity func where the return type of the func differs from the type of the domain entity on which it is called and returns a list, and return the result of the domain entity func mapped to a list of output DTOs.
        /// </summary>
        /// <typeparam name="TDomainEntityResult">The type of the result of the domain entity func.</typeparam>
        /// <typeparam name="TOutputDTO">The type of the output DTO, to which the result of the domain entity func will be mapped.</typeparam>
        /// <param name="id">The Id of the domain entity on which to call the domain entity func.</param>
        /// <param name="domainMethodFuncAsync">The domain entity func to call.</param>
        /// <param name="lookupFuncAsync">An optional repository lookup func, for including different child relationships.</param>
        /// <returns>The result of the domain entity func mapped to an output DTO.</returns>
        Task<OperationResult<List<TOutputDTO>>> LookupEntityThenCallDomainMethodSaveAndMapAsync<TDomainEntityResult, TOutputDTO>(
            int id,
            Func<TDomainEntity, TUnitOfWork, Task<OperationResult<List<TDomainEntityResult>>>> domainMethodFuncAsync,
            Func<TRepository, int, Task<TDomainEntity>> lookupFuncAsync = null)
            where TDomainEntityResult : class, IIdentity
            where TOutputDTO : class;

        Task<OperationResult> ExecuteWrappedTryCatchLogAsync(
            Func<Task<OperationResult>> func,
            string errorMessage = "Error running business logic method",
            Func<Exception, Task> onExceptionFuncAsync = null);

        Task<OperationResult<T>> ExecuteWrappedTryCatchLogAsync<T>(
            Func<Task<OperationResult<T>>> func,
            string errorMessage = "Error running business logic method",
            Func<Exception, Task> onExceptionFuncAsync = null);
    }
}