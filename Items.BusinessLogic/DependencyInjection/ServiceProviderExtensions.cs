using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Common.ExtensionMethods;
using Common.Utility;
using Items.Data.EFCore.Abstraction.Interfaces;
using Items.Data.EFCore.Entities.Interfaces;
using Items.GenericServices.Interfaces;
using Items.Domain.DomainRepositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Items.BusinessLogic.DependencyInjection
{
    public static class ServiceProviderExtensions
    {
        private static readonly Lazy<Dictionary<Type, Type>> LazyBusinessLogicServicesByType = new Lazy<Dictionary<Type, Type>>(() =>
        {
            var serviceTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => x.IsClass
                    && x.Namespace?.StartsWith(new[] { nameof(Items), nameof(BusinessLogic), nameof(Services) }.StringJoin(".")) == true
                    && x.Namespace?.Contains("CustomerSpecific", StringComparison.OrdinalIgnoreCase) == false) // ignore customer-specific business logic classes, as they may also wrap the same domain entity as the main business logic class for an entity
                .Select(x =>
                {
                    var implementations = TypeUtility.GetGenericInterfaceImplementations(x, typeof(IGenericBusinessLogic<,,>));

                    if (implementations.Any()
                        // filter out generic type contraint ones - they have no full name
                        && !string.IsNullOrWhiteSpace(implementations[0].FullName))
                    {
                        return (
                            implementations[0].GetGenericArguments()[0],
                            x.GetTypeInfo().ImplementedInterfaces.SingleOrDefault(y => y.Name == $"I{x.Name}"));
                    }

                    return (null, null);
                })
                .Where(x => x.Item1 != null)
                .OrderBy(x => x.Item1.Name);

            var duplicatedServiceTypes = serviceTypes
                .GroupBy(x => x.Item1)
                .Where(x => x.Count() > 1)
                .ToList();

            if (duplicatedServiceTypes.Any())
            {
                throw new InvalidOperationException($"The following domain entity types were detected as having more than one associated business logic class.\r\nIt is expected that each domain entity type only has one main associated business logic class.\r\nIf you need to add another for a customer-specific requirement, make sure it is in BusinessLogic\\Services\\CustomerSpecific folder, and it will be excluded from this check.\r\n{duplicatedServiceTypes.Select(x => x.Key.FullName).StringJoin(", ")}");
            }

            return serviceTypes.ToDictionary(x => x.Item1, x => x.Item2);
        });

        public static IGenericBusinessLogic<TDomainEntity, IItemsUnitOfWork, IBaseRepository<TDomainEntity>> GetDomainEntityBusinessLogicService<TDomainEntity>(this IServiceProvider serviceProvider)
            where TDomainEntity : class, IIdentity
        {
            if (!LazyBusinessLogicServicesByType.Value.TryGetValue(typeof(TDomainEntity), out var serviceType) || serviceType == null)
            {
                throw new InvalidOperationException($"There is no domain business logic service registered for type {typeof(TDomainEntity).Name}");
            }

            return serviceProvider.GetRequiredService(serviceType) as IGenericBusinessLogic<TDomainEntity, IItemsUnitOfWork, IBaseRepository<TDomainEntity>>;
        }
    }
}
