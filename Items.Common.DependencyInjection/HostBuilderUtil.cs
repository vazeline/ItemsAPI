using Common.Utility;
using Common.Utility.Classes.DependencyInjection;
using Common.Utility.Classes.DependencyInjection.Interfaces;
using Items.Common.WebAPI.Utility;
using Items.Data.EFCore.DependencyInjection;
using Items.Data.EFCore.Utility;
using Items.Domain;
using Items.Domain.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Items.BusinessLogic.DependencyInjection;

namespace Items.Common.DependencyInjection
{
    public class HostBuilderUtil
    {
        public static IHostBuilder ConfigureHostBuilderCommon(IHostBuilder builder)
        {
            builder.UseSerilog();

            return builder;
        }

        public static void ConfigureServicesCommon(
            IServiceCollection services,
            IConfiguration configuration,
            IMvcBuilder mvcBuilder = null)
        {
            services.AddLogging(builder => builder.AddSerilog(dispose: true));

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .CreateLogger();

            services.AddScoped<ILazyDependencyResolver, LazyDependencyResolver>();

            services.AddAutoMapper(
                configAction: cfg =>
                {
                    cfg.ShouldMapProperty = x => x.GetMethod.IsPublic || x.GetMethod.IsAssembly; // map public and internal properties
                },
                assemblies: AssemblyUtility.GetProjectAndItemsAssemblies(rootSolutionNamespaceToSearch: "Items"));

            services.AddDbContextForSqlServer<ItemsContext>(
                configuration: configuration,
                connectionStringName: "ItemsConnection");

            services.AddItemsDomainServices(configuration);
            services.AddItemsBusinessLogicServices();
        }

        public static void ConfigureAppCommon<TDbContext>(IHost app, bool useLogicanTemplatingEngine)
            where TDbContext : DbContext
        {
            using (var serviceScope = app.Services.CreateScope())
            {
                var loggerFactory = serviceScope.ServiceProvider.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger(typeof(HostBuilderUtil));

                MappingHostBuilderUtility.ValidateMappings(app.Services, logger);
                DataHostBuilderUtility.UpgradeDatabase<TDbContext>(app.Services, logger);
            }

            ControllerUtility.DomainEntityNamespace = $"{nameof(Items)}.{nameof(Domain)}";
        }
    }
}
