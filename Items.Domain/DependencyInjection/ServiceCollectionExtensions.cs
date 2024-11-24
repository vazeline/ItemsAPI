using System;
using Common.Enums.DependencyInjection;
using Items.Domain.DomainRepositories;
using Items.Domain.DomainRepositories.Actions;
using Items.Domain.DomainRepositories.Actions.Interfaces;
using Items.Domain.DomainRepositories.Interfaces;
using Items.Domain.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Items.Domain.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static void AddItemsDomainServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IItemsUnitOfWork, ItemsUnitOfWork>();

            // main repositories
            services.AddScoped<IAuditLogRepository, ItemAuditLogRepository>();
            services.AddScoped<IItemRepository, ItemRepository>();

            services.Configure<ConfigurationSettings>(configuration.GetSection("ConfigurationSettings"));
        }
    }
}
