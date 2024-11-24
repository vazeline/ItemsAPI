using Items.BusinessLogic.Services.Actions;
using Items.BusinessLogic.Services.Actions.Interfaces;
using Items.BusinessLogic.Services.AuditLog.Interfaces;
using Items.Domain.DomainEntityBehaviours.Actions;
using Microsoft.Extensions.DependencyInjection;

namespace Items.BusinessLogic.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static void AddItemsBusinessLogicServices(this IServiceCollection services)
        {
            // business logic services
            services.AddScoped<IItemBusinessLogic, ItemBusinessLogic>();
            services.AddScoped<IAuditLogBusinessLogic, AuditLogBusinessLogic>();
        }
    }
}
