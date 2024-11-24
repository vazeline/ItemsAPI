using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Items.Data.EFCore.Entities;

namespace Items.Data.EFCore.Utility
{
    public static class ServiceUtility
    {
        /// <summary>
        /// Mechanism to provide a specific service provider (which will implicityly have been resolved from a scope) to domain entities
        /// for their service resolution from the DI container.
        /// For web apps, this is unnecessary since each incoming client request creates a new service scope.
        /// However, for other long-running hosts (eg console app, test host),there is only a single top-level scope for entire app lifetime.
        /// This is often undesired, and we will need to create new scopes as required. They can then be used with this method.
        /// </summary>
        public static void UseServiceProviderForDomainEntityServiceAccessDuringAction(IServiceProvider serviceProvider, Action action)
        {
            var originalDomainEntityBaseGetCurrentServiceProviderFunc = DomainEntityBase.CurrentServiceProviderFunc;
            DomainEntityBase.SetCurrentServiceProviderFunc(() => serviceProvider);

            try
            {
                action();
            }
            finally
            {
                DomainEntityBase.SetCurrentServiceProviderFunc(originalDomainEntityBaseGetCurrentServiceProviderFunc);
            }
        }

        /// <summary>
        /// Mechanism to provide a specific service provider (which will implicityly have been resolved from a scope) to domain entities
        /// for their service resolution from the DI container.
        /// For web apps, this is unnecessary since each incoming client request creates a new service scope.
        /// However, for other long-running hosts (eg console app, test host),there is only a single top-level scope for entire app lifetime.
        /// This is often undesired, and we will need to create new scopes as required. They can then be used with this method.
        /// </summary>
        public static async Task UseServiceProviderForDomainEntityServiceAccessDuringActionAsync(IServiceProvider serviceProvider, Func<Task> actionAsync)
        {
            var originalDomainEntityBaseGetCurrentServiceProviderFunc = DomainEntityBase.CurrentServiceProviderFunc;
            DomainEntityBase.SetCurrentServiceProviderFunc(() => serviceProvider);

            try
            {
                await actionAsync();
            }
            finally
            {
                DomainEntityBase.SetCurrentServiceProviderFunc(originalDomainEntityBaseGetCurrentServiceProviderFunc);
            }
        }
    }
}
