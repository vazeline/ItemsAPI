using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Enums.DependencyInjection;
using Items.Data.EFCore.Entities;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Items.Common.Testing.Utility
{
    public class TestServiceUtility
    {
        public static void ReplaceRegisteredServiceWithMock<TServiceInterface>(
            IServiceCollection services,
            Action<Mock<TServiceInterface>> mockSetupAction,
            ServiceRegistrationType serviceRegistrationType = ServiceRegistrationType.Scoped)
            where TServiceInterface : class
        {
            var existing = services.SingleOrDefault(d => d.ServiceType == typeof(TServiceInterface));

            if (existing == null)
            {
                throw new InvalidOperationException($"No service of type {typeof(TServiceInterface).Name} is registered");
            }

            var mockService = new Mock<TServiceInterface>();

            mockSetupAction(mockService);

            switch (serviceRegistrationType)
            {
                case ServiceRegistrationType.Scoped:
                    services.AddScoped(_ => mockService.Object);
                    break;
                case ServiceRegistrationType.Singleton:
                    services.AddSingleton(_ => mockService.Object);
                    break;
                case ServiceRegistrationType.Transient:
                    services.AddTransient(_ => mockService.Object);
                    break;
                default:
                    throw new NotSupportedException(serviceRegistrationType.ToString());
            }
        }
    }
}
