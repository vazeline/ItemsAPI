using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.ExtensionMethods;
using Items.Data.EFCore.Entities.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Items.Data.EFCore.Entities
{
    public abstract class DomainEntityBase
    {
        /// <summary>
        /// Allows domain entities to resolve request services from the given service provider. Should be populated in the application entry point.
        /// For web applications, value should be populated in the application entry point and resolve from IHttpContextAccessor.RequestServices,
        /// so that it is correctly scoped for each request.
        /// For console applications/unit tests, value can resolve either from the root level app.Services (being aware this is a single scoep for the lifetime of the app),
        /// or you shoud create scopes as required and set from that.
        /// NOTE - UnitOfWork/DbContext is unique per scope.
        /// </summary>
        internal static Func<IServiceProvider> CurrentServiceProviderFunc { get; private set; }

        public static void SetCurrentServiceProviderFunc(Func<IServiceProvider> func) => CurrentServiceProviderFunc = func;

        public static string GetNonExistentOrUnauthorisedEntityMessage<TEntity>(int? id = null) => $"{typeof(TEntity).Name}{(id == null ? string.Empty : $" with Id {id}")} does not exist or you do not have the required permissions";

        protected static void UpdateDependentListWithNewMembers<TDependent>(List<TDependent> dependentList, List<TDependent> newMembers)
            where TDependent : IIdentity
        {
            dependentList.ThrowIfNull();
            newMembers.ThrowIfNull();

            var existingIds = dependentList.Select(x => x.Id).ToList();
            var newIds = newMembers.Select(x => x.Id ).ToList();

            var removedIds = existingIds.Except(newIds).ToList();

            if (removedIds.Any())
            {
                for (var i = dependentList.Count - 1; i >= 0; i--)
                {
                    if (removedIds.Contains(dependentList[i].Id ) )
                    {
                        dependentList.RemoveAt(i);
                    }
                }
            }

            var addedIds = newIds.Except(existingIds).ToList();

            if (addedIds.Any())
            {
                dependentList.AddRange(newMembers.Where(x => addedIds.Contains(x.Id ) ));
            }
        }

        protected static TService GetServiceFromCurrentServiceProvider<TService>()
        {
            var serviceProvider = CurrentServiceProviderFunc();
            serviceProvider.ThrowIfNull("Could not resolve service provider - are you on a code path outside of a HttpContext?");

            return serviceProvider.GetRequiredService<TService>();
        }
    }
}
